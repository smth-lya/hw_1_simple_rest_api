using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Services.Kafka;
using HW1.Api.Domain.Contracts.Telegram;
using Telegram.Bot.Types;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public abstract class BaseCommandHandler : ICommandHandler
{
    protected readonly ITelegramBotService _botService;
    protected readonly IUserService _userService;
    protected readonly ITelegramUserService _telegramUserService;
    protected readonly IAnalyticsService _analyticsService;
    protected readonly ILogger _logger;

    protected BaseCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService,
        IAnalyticsService analyticsService,
        ILogger logger)
    {
        _botService = botService;
        _userService = userService;
        _telegramUserService = telegramUserService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    public abstract string Command { get; }
    public abstract string Description { get; }
    
    public abstract Task HandleAsync(Message message, CancellationToken cancellationToken);
    
    public virtual Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected async Task<bool> ValidateUserAccessAsync(long telegramUserId, CancellationToken cancellationToken)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "ValidateUserAccess",
            ["TelegramUserId"] = telegramUserId
        });

        var user = await _telegramUserService.GetUserAsync(telegramUserId);
        var isValid = user is { IsActive: true };

        _logger.LogDebug("User access validation result: {IsValid} for user {TelegramUserId}", 
            isValid, telegramUserId);

        return isValid;
    }

    protected IDisposable BeginCommandScope(Message message, string operation = "HandleCommand")
    {
        return _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = Command,
            ["Operation"] = operation,
            ["MessageId"] = message.MessageId,
            ["UserId"] = message.From?.Id,
            ["ChatId"] = message.Chat.Id,
            ["UserName"] = message.From?.Username ?? "Unknown"
        });
    }

    protected IDisposable BeginCallbackScope(CallbackQuery callbackQuery, string operation = "HandleCallback")
    {
        return _logger.BeginScope(new Dictionary<string, object>
        {
            ["Command"] = Command,
            ["Operation"] = operation,
            ["CallbackQueryId"] = callbackQuery.Id,
            ["UserId"] = callbackQuery.From.Id,
            ["ChatId"] = callbackQuery.Message?.Chat.Id,
            ["CallbackData"] = callbackQuery.Data
        });
    }
}