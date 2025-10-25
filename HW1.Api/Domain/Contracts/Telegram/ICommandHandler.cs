using Telegram.Bot.Types;

namespace HW1.Api.Domain.Contracts.Telegram;

public interface ICommandHandler
{
    string Command { get; }
    string Description { get; }
    Task HandleAsync(Message message, CancellationToken cancellationToken);
    Task HandleCallbackAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken);
}