using Telegram.Bot.Types.ReplyMarkups;

namespace HW1.Api.Domain.Contracts.Telegram;

public interface ITelegramBotService
{
    Task RunAsync(CancellationToken cancellationToken);
    Task SendMessageAsync(long chatId, string message, ReplyMarkup? replyMarkup = null, CancellationToken cancellationToken = default);
    Task AnswerCallbackQuery(string callbackQueryId, string? text = null, bool showAlert = false, string? url = null, int? cacheTime = null, CancellationToken cancellationToken = default);
    Task BroadcastMessageAsync(string message, CancellationToken cancellationToken = default);
}