using HW1.Api.Domain.Contracts.Telegram;
using HW1.Api.Domain.Models;
using HW1.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HW1.Api.Infrastructure.Telegram;

public class TelegramUserService : ITelegramUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TelegramUserService> _logger;

    public TelegramUserService(ApplicationDbContext context, ILogger<TelegramUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TelegramUser?> GetUserAsync(long telegramUserId)
    {
        return await _context.Set<TelegramUser>()
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);
    }

    public async Task<TelegramUser> RegisterUserAsync(long telegramUserId, long chatId, string username, string firstName, string lastName)
    {
        var existingUser = await GetUserAsync(telegramUserId);
        
        if (existingUser != null)
        {
            existingUser.LastActivity = DateTime.UtcNow;
            existingUser.IsActive = true;
            await _context.SaveChangesAsync();
            return existingUser;
        }

        var newUser = new TelegramUser
        {
            TelegramUserId = telegramUserId,
            ChatId = chatId,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            RegisteredAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            IsActive = true
        };

        _context.Set<TelegramUser>().Add(newUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New Telegram user registered: {UserId} ({Username})", telegramUserId, username);
        
        return newUser;
    }

    public async Task UpdateUserActivityAsync(long telegramUserId)
    {
        var user = await GetUserAsync(telegramUserId);
        if (user != null)
        {
            user.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetActiveUsersCountAsync()
    {
        return await _context.Set<TelegramUser>()
            .Where(u => u.IsActive)
            .CountAsync();
    }

    public async Task<bool> IsUserRegisteredAsync(long telegramUserId)
    {
        return await _context.Set<TelegramUser>()
            .AnyAsync(u => u.TelegramUserId == telegramUserId && u.IsActive);
    }
}