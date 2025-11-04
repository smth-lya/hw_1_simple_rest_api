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
    private readonly Func<IEnumerable<ICommandHandler>> _commandHandlersFactory; 

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public TelegramBotService(
        IOptions<TelegramBotConfiguration> config,
        ILogger<TelegramBotService> logger,
        IServiceProvider serviceProvider,
        Func<IEnumerable<ICommandHandler>> commandHandlersFactory)
    {
        _config = config.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _commandHandlersFactory = commandHandlersFactory;
        
        _botClient = new TelegramBotClient(_config.BotToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram Bot...");
        
        await StartPollingAsync(cancellationToken);
    }
    private async Task StartPollingAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],        // типы обновлений, которые бот будет слушать
            DropPendingUpdates = true,                                              // удаление старых сообщений, которые пришли, пока бот был офлайн
        };

        await _botClient.ReceiveAsync(
            this,           // объект, который умеет обрабатывать обновления
            receiverOptions,            
            cancellationToken);         // для остановки бота
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            await (update.Type switch
            {
                UpdateType.Message => OnMessageReceived(update.Message!),
                UpdateType.CallbackQuery => OnCallbackQueryReceived(update.CallbackQuery!),
                _ => Task.CompletedTask
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
        }
    }
    private async Task OnMessageReceived(Message message)
    {
        if (message.Text is not { } messageText)
            return;

        _logger.LogInformation("Received message from {UserId}: {Text}", message.From?.Id, messageText);

        using var scope = _serviceProvider.CreateScope();
    
        // проверяем, находится ли пользователь в процессе регистрации
        var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterCommandHandler>();
    
        if (await registerHandler.IsUserInRegistrationAsync(message.From!.Id) && !messageText.StartsWith($"/"))
        {
            await registerHandler.HandleRegistrationStepAsync(message, _cancellationTokenSource.Token);
            return;
        }

        var commandHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<ICommandHandler>>();
        var commandHandler = commandHandlers.FirstOrDefault(handler => 
            messageText.StartsWith(handler.Command, StringComparison.OrdinalIgnoreCase));

        if (commandHandler != null)
        {
            await commandHandler.HandleAsync(message, _cancellationTokenSource.Token);
        }
        else
        {
            await HandleUnknownCommand(message);
        }
    }
    private async Task OnCallbackQueryReceived(CallbackQuery callbackQuery)
    {
        _logger.LogInformation("Received callback from {UserId}: {Data}", 
            callbackQuery.From.Id, callbackQuery.Data);

        using var scope = _serviceProvider.CreateScope();
        var commandHandlers = scope.ServiceProvider.GetRequiredService<IEnumerable<ICommandHandler>>();

        var commandHandler = commandHandlers.FirstOrDefault(handler => 
            callbackQuery.Data?.StartsWith(handler.Command) == true);

        await AnswerCallbackQuery(callbackQuery.Id);

        if (commandHandler != null)
        {
            await commandHandler.HandleCallbackAsync(callbackQuery, _cancellationTokenSource.Token);
        }
    }
    private async Task HandleUnknownCommand(Message message)
    {
        const string response = """
                                Неизвестная команда.
                                Для просмотра доступных команд используйте /help
                                """;

        await SendMessageAsync(message.Chat.Id, response);
    }
    
    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => 
                $"Telegram API Error: [{apiRequestException.ErrorCode}] {apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, "Polling error occurred");

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
    
    public async Task SendMessageAsync(long chatId, string message, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message.Length > _config.MaxMessageLength)
            {
                message = message[.._config.MaxMessageLength] + "...";
            }

            await _botClient.SendMessage(
                chatId,
                message,
                parseMode: ParseMode.Html,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Error sending message to chat {ChatId}", chatId);
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
        await _botClient.AnswerCallbackQuery(callbackQueryId, text, showAlert, url, cacheTime, cancellationToken);
    }

    public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Broadcast message: {Message}", message);
        await Task.CompletedTask;
    }
}