using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.Infrastructure.Telegram;

public class TelegramBotService : ITelegramBotService, IUpdateHandler
{
    private readonly TelegramBotClient _botClient;
    private readonly TelegramBotConfiguration _config;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public TelegramBotService(
        IOptions<TelegramBotConfiguration> config,
        ILogger<TelegramBotService> logger,
        IServiceProvider serviceProvider)
    {
        _config = config.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        _botClient = new TelegramBotClient(_config.BotToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramBotService",
            ["Operation"] = "RunAsync"
        });
        
        _logger.LogInformation("Starting Telegram Bot polling");
        
        await StartPollingAsync(cancellationToken);
    }
    private async Task StartPollingAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],        // типы обновлений, которые бот будет слушать
            DropPendingUpdates = true,                                              // удаление старых сообщений, которые пришли, пока бот был офлайн
        };

        _logger.LogInformation("Bot polling started with options: {@ReceiverOptions}", receiverOptions);
        
        await _botClient.ReceiveAsync(
            this,           // объект, который умеет обрабатывать обновления
            receiverOptions,            
            cancellationToken);         // для остановки бота
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["UpdateId"] = update.Id,
            ["UpdateType"] = update.Type.ToString(),
            ["UserId"] = GetUserIdFromUpdate(update),
            ["ChatId"] = GetChatIdFromUpdate(update)
        });

        
        try
        {
            _logger.LogDebug("Processing update {UpdateId} of type {UpdateType}", update.Id, update.Type);
            
            await (update.Type switch
            {
                UpdateType.Message => OnMessageReceived(update.Message!),
                UpdateType.CallbackQuery => OnCallbackQueryReceived(update.CallbackQuery!),
                _ => Task.CompletedTask
            });
            
            stopwatch.Stop();
            
            _logger.LogInformation("Update {UpdateId} processed successfully in {ElapsedMs}ms", 
                update.Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Error processing update {UpdateId}. Duration: {ElapsedMs}ms", 
                update.Id, stopwatch.ElapsedMilliseconds);
        }
    }
    private async Task OnMessageReceived(Message message)
    {
        if (message.Text is not { } messageText)
            return;

        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["MessageId"] = message.MessageId,
            ["MessageType"] = "Text",
            ["UserId"] = message.From?.Id,
            ["ChatId"] = message.Chat.Id,
            ["UserName"] = message.From?.Username ?? "Unknown",
            ["MessageLength"] = messageText.Length
        });
        
        _logger.LogInformation("Received message from user {UserId} in chat {ChatId}: {MessageText}", 
            message.From?.Id, message.Chat.Id, messageText);
        
        using var scope = _serviceProvider.CreateScope();
    
        // проверяем, находится ли пользователь в процессе регистрации
        var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterCommandHandler>();
    
        if (await registerHandler.IsUserInRegistrationAsync(message.From!.Id) && !messageText.StartsWith($"/"))
        {
            _logger.LogDebug("User {UserId} is in registration process, handling registration step", message.From.Id);

            await registerHandler.HandleRegistrationStepAsync(message, _cancellationTokenSource.Token);
            return;
        }

        var commandHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<ICommandHandler>>();
        var commandHandler = commandHandlers.FirstOrDefault(handler => 
            messageText.StartsWith(handler.Command, StringComparison.OrdinalIgnoreCase));

        if (commandHandler != null)
        {
            _logger.LogInformation("Executing command {Command} for user {UserId}", 
                commandHandler.Command, message.From.Id);
            
            await commandHandler.HandleAsync(message, _cancellationTokenSource.Token);
        }
        else
        {
            _logger.LogWarning("Unknown command received from user {UserId}: {MessageText}", 
                message.From.Id, messageText);
            
            await HandleUnknownCommand(message);
        }
    }
    private async Task OnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CallbackQueryId"] = callbackQuery.Id,
            ["UserId"] = callbackQuery.From.Id,
            ["ChatId"] = callbackQuery.Message?.Chat.Id,
            ["CallbackData"] = callbackQuery.Data
        });
        
        _logger.LogInformation("Received callback from user {UserId} with data: {CallbackData}", 
            callbackQuery.From.Id, callbackQuery.Data);

        using var scope = _serviceProvider.CreateScope();
        var commandHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<ICommandHandler>>();

        var commandHandler = commandHandlers.FirstOrDefault(handler => 
            callbackQuery.Data?.StartsWith(handler.Command) == true);

        await AnswerCallbackQuery(callbackQuery.Id);

        if (commandHandler != null)
        {
            _logger.LogInformation("Executing callback handler for command {Command}", commandHandler.Command);
            
            await commandHandler.HandleCallbackAsync(callbackQuery, _cancellationTokenSource.Token);
            return;
        }
        
        _logger.LogWarning("No handler found for callback data: {CallbackData}", callbackQuery.Data);
    }
    private async Task HandleUnknownCommand(Message message)
    {
        const string response = """
                                Неизвестная команда.
                                Для просмотра доступных команд используйте /help
                                """;

        _logger.LogDebug("Sending unknown command response to user {UserId}", message.From?.Id);
        
        await SendMessageAsync(message.Chat.Id, response);
    }
    
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ErrorSource"] = source.ToString(),
            ["ExceptionType"] = exception.GetType().Name
        });
        
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => 
                $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, 
            "Telegram API error from source {ErrorSource}: {ErrorMessage}", 
            source, errorMessage);
        
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    
    public async Task SendMessageAsync(long chatId, string message, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ChatId"] = chatId,
            ["MessageLength"] = message.Length,
            ["HasReplyMarkup"] = replyMarkup != null
        });
        
        try
        {
            if (message.Length > _config.MaxMessageLength)
            {
                _logger.LogWarning("Message truncated from {OriginalLength} to {MaxLength} characters", 
                    message.Length, _config.MaxMessageLength);
                
                message = message[.._config.MaxMessageLength] + "...";
            }

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Message sent to chat {ChatId} successfully in {ElapsedMs}ms", 
                chatId, stopwatch.ElapsedMilliseconds);
        }
        catch (ApiRequestException ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Failed to send message to chat {ChatId} after {ElapsedMs}ms. Error: {TelegramErrorCode}", 
                chatId, stopwatch.ElapsedMilliseconds, ex.ErrorCode);
        }
    }

    public async Task AnswerCallbackQuery(
        string callbackQueryId,
        string? text = null,
        bool showAlert = false,
        string? url = null,
        int? cacheTime = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CallbackQueryId"] = callbackQueryId,
            ["HasText"] = !string.IsNullOrEmpty(text),
            ["ShowAlert"] = showAlert
        });
        
        try
        {
            await _botClient.AnswerCallbackQuery(callbackQueryId, text, showAlert, url, cacheTime, cancellationToken);
            _logger.LogDebug("Callback query {CallbackQueryId} answered successfully", callbackQueryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer callback query {CallbackQueryId}", callbackQueryId);
        }
    }

    public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "BroadcastMessage",
            ["MessageLength"] = message.Length
        });

        _logger.LogInformation("Broadcast message initiated: {MessageLength} characters", message.Length);
        await Task.CompletedTask;
    }
    
    private static long? GetUserIdFromUpdate(Update update) => update.Type switch
    {
        UpdateType.Message => update.Message?.From?.Id,
        UpdateType.CallbackQuery => update.CallbackQuery?.From.Id,
        _ => null
    };

    private static long? GetChatIdFromUpdate(Update update) => update.Type switch
    {
        UpdateType.Message => update.Message?.Chat.Id,
        UpdateType.CallbackQuery => update.CallbackQuery?.Message?.Chat.Id,
        _ => null
    };
}