using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram Bot...");

        if (_config.UseWebhook)
        {
            await SetWebhookAsync(cancellationToken);
        }
        else
        {
            await StartPollingAsync(cancellationToken);
        }

        _logger.LogInformation("Telegram Bot started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram Bot...");
        
        await _cancellationTokenSource.CancelAsync();
        
        if (_config.UseWebhook)
        {
            await _botClient.DeleteWebhook(cancellationToken: cancellationToken);
        }

        _logger.LogInformation("Telegram Bot stopped");
    }

    public async Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
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
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Error sending message to chat {ChatId}", chatId);
        }
    }

    public async Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        // массовая рассылка
        _logger.LogInformation("Broadcast message: {Message}", message);
        await Task.CompletedTask;
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

    private async Task OnMessageReceived(Message message)
    {
        if (message.Text is not { } messageText)
            return;

        _logger.LogInformation("Received message from {UserId}: {Text}", message.From?.Id, messageText);

        using var scope = _serviceProvider.CreateScope();
    
        // Проверяем, находится ли пользователь в процессе регистрации
        var registerHandler = scope.ServiceProvider.GetRequiredService<RegisterCommandHandler>();
    
        // Если пользователь в процессе регистрации, обрабатываем шаг
        if (await registerHandler.IsUserInRegistrationAsync(message.From.Id) && 
            !messageText.StartsWith("/"))
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

        if (commandHandler != null)
        {
            await commandHandler.HandleCallbackAsync(callbackQuery, _cancellationTokenSource.Token);
        }
    }

    private async Task HandleUnknownCommand(Message message)
    {
        var response = @"
        Неизвестная команда.
        Для просмотра доступных команд используйте /help
        ".Trim();

        await SendMessageAsync(message.Chat.Id, response);
    }

    private async Task SetWebhookAsync(CancellationToken cancellationToken)
    {
        if (!_config.UseWebhook || string.IsNullOrEmpty(_config.WebhookUrl))
        {
            _logger.LogWarning("Webhook is not configured");
            return;
        }

        try
        {
            await _botClient.SetWebhook(
                url: _config.WebhookUrl,
                allowedUpdates: Array.Empty<UpdateType>(),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Webhook set to: {WebhookUrl}", _config.WebhookUrl);
            
            var webhookInfo = await _botClient.GetWebhookInfo(cancellationToken);
            _logger.LogInformation("Webhook info: {Url}, Pending updates: {PendingUpdatesCount}", 
                webhookInfo.Url, webhookInfo.PendingUpdateCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set webhook");
            throw;
        }
    }

    private async Task StartPollingAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = true,
        };

        await _botClient.ReceiveAsync(
            this,
            receiverOptions,
            cancellationToken);
    }
}