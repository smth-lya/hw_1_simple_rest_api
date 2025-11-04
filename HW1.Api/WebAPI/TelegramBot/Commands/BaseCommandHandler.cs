using HW1.Api.Domain.Contracts.Services;
using HW1.Api.Domain.Contracts.Telegram;
using Telegram.Bot.Types;

namespace HW1.Api.WebAPI.TelegramBot.Commands;

public abstract class BaseCommandHandler : ICommandHandler
{
    protected readonly ITelegramBotService _botService;
    protected readonly IUserService _userService;
    protected readonly ITelegramUserService _telegramUserService;

    protected BaseCommandHandler(
        ITelegramBotService botService,
        IUserService userService,
        ITelegramUserService telegramUserService)
    {
        _botService = botService;
        _userService = userService;
        _telegramUserService = telegramUserService;
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
        var user = await _telegramUserService.GetUserAsync(telegramUserId);
        return user is { IsActive: true };
    }
}