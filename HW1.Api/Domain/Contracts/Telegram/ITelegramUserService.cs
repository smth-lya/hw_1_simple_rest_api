using HW1.Api.Domain.Models;

namespace HW1.Api.Domain.Contracts.Telegram;

public interface ITelegramUserService
{
    Task<TelegramUser?> GetUserAsync(long telegramUserId);
    Task<TelegramUser> RegisterUserAsync(long telegramUserId, long chatId, string username, string firstName, string lastName);
    Task UpdateUserActivityAsync(long telegramUserId);
    Task<int> GetActiveUsersCountAsync();
    Task<bool> IsUserRegisteredAsync(long telegramUserId);
}