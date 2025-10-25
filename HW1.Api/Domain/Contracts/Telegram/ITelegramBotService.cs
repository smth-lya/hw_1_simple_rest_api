namespace HW1.Api.Domain.Contracts.Telegram;

public interface ITelegramBotService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default);
    Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default);
}