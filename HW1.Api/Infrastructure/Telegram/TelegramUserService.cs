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

        _context.TelegramUsers.Add(newUser);
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

    public async Task LinkToSystemUserAsync(long telegramUserId, Guid systemUserId)
    {
        try
        {
            var telegramUser = await _context.TelegramUsers
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

            if (telegramUser == null)
            {
                throw new InvalidOperationException($"Telegram user {telegramUserId} not found");
            }

            // Проверяем, существует ли системный пользователь
            var systemUser = await _context.Users
                .AnyAsync(u => u.UserId == systemUserId);
                
            if (!systemUser)
            {
                throw new InvalidOperationException($"System user {systemUserId} not found");
            }

            // Проверяем, не привязан ли уже этот Telegram аккаунт к другому пользователю
            var existingLink = await _context.TelegramUsers
                .AnyAsync(u => u.SystemUserId == systemUserId && u.TelegramUserId != telegramUserId);
                
            if (existingLink)
            {
                throw new InvalidOperationException($"System user {systemUserId} is already linked to another Telegram account");
            }

            telegramUser.SystemUserId = systemUserId;
            telegramUser.LastActivity = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Telegram user {TelegramUserId} linked to system user {SystemUserId}", 
                telegramUserId, systemUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking Telegram user {TelegramUserId} to system user {SystemUserId}", 
                telegramUserId, systemUserId);
            throw;
        }
    }

    public async Task UnlinkFromSystemUserAsync(long telegramUserId)
    {
        var telegramUser = await _context.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

        if (telegramUser != null && telegramUser.SystemUserId.HasValue)
        {
            telegramUser.SystemUserId = null;
            telegramUser.LastActivity = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Telegram user {TelegramUserId} unlinked from system user", telegramUserId);
        }
    }
}