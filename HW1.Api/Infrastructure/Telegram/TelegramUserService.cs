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
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "GetUserAsync",
            ["TelegramUserId"] = telegramUserId
        });
        
        _logger.LogDebug("Fetching Telegram user {TelegramUserId}", telegramUserId);
        
        var user = await _context.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

        if (user == null)
        {
            _logger.LogDebug("Telegram user {TelegramUserId} not found", telegramUserId);
        }
        else
        {
            _logger.LogDebug("Telegram user {TelegramUserId} found: {UserName}", telegramUserId, user.Username);
        }

        return user;
    }

    public async Task<TelegramUser> RegisterUserAsync(long telegramUserId, long chatId, string username, string firstName, string lastName)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "RegisterUserAsync",
            ["TelegramUserId"] = telegramUserId,
            ["ChatId"] = chatId,
            ["UserName"] = username
        });
        
        var existingUser = await GetUserAsync(telegramUserId);
        
        if (existingUser != null)
        {
            _logger.LogInformation("Existing Telegram user {TelegramUserId} updated activity", telegramUserId);
            
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

        _logger.LogInformation(
            "New Telegram user registered: UserId={TelegramUserId}, Username={UserName}, ChatId={ChatId}", 
            telegramUserId, username, chatId);
        
        return newUser;
    }

    public async Task UpdateUserActivityAsync(long telegramUserId)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "UpdateUserActivityAsync",
            ["TelegramUserId"] = telegramUserId
        });

        var user = await GetUserAsync(telegramUserId);
        if (user != null)
        {
            user.LastActivity = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogDebug("User activity updated for {TelegramUserId}", telegramUserId);
        }
        else
        {
            _logger.LogWarning("Attempted to update activity for non-existent user {TelegramUserId}", telegramUserId);
        }
    }

    public async Task<int> GetActiveUsersCountAsync()
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "GetActiveUsersCountAsync"
        });

        var count = await _context.TelegramUsers
            .Where(u => u.IsActive)
            .CountAsync();

        _logger.LogInformation("Active users count: {ActiveUsersCount}", count);
        
        return count;
    }

    public async Task<bool> IsUserRegisteredAsync(long telegramUserId)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "IsUserRegisteredAsync",
            ["TelegramUserId"] = telegramUserId
        });

        var isRegistered = await _context.TelegramUsers
            .AnyAsync(u => u.TelegramUserId == telegramUserId && u.IsActive);

        _logger.LogDebug("User {TelegramUserId} registration status: {IsRegistered}", 
            telegramUserId, isRegistered);
            
        return isRegistered;
    }

    public async Task LinkToSystemUserAsync(long telegramUserId, Guid systemUserId)
    {
         using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "LinkToSystemUserAsync",
            ["TelegramUserId"] = telegramUserId,
            ["SystemUserId"] = systemUserId
        });

        try
        {
            var telegramUser = await _context.TelegramUsers
                .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

            if (telegramUser == null)
            {
                _logger.LogError("Telegram user {TelegramUserId} not found for linking", telegramUserId);
                throw new InvalidOperationException($"Telegram user {telegramUserId} not found");
            }

            // Проверяем, существует ли системный пользователь
            var systemUser = await _context.Users
                .AnyAsync(u => u.UserId == systemUserId);
                
            if (!systemUser)
            {
                _logger.LogError("System user {SystemUserId} not found for linking", systemUserId);
                throw new InvalidOperationException($"System user {systemUserId} not found");
            }

            // Проверяем, не привязан ли уже этот Telegram аккаунт к другому пользователю
            var existingLink = await _context.TelegramUsers
                .AnyAsync(u => u.SystemUserId == systemUserId && u.TelegramUserId != telegramUserId);
                
            if (existingLink)
            {
                _logger.LogWarning("System user {SystemUserId} is already linked to another Telegram account", systemUserId);
                throw new InvalidOperationException($"System user {systemUserId} is already linked to another Telegram account");
            }

            telegramUser.SystemUserId = systemUserId;
            telegramUser.LastActivity = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Successfully linked Telegram user {TelegramUserId} to system user {SystemUserId}", 
                telegramUserId, systemUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to link Telegram user {TelegramUserId} to system user {SystemUserId}", 
                telegramUserId, systemUserId);
            throw;
        }
    }

    public async Task UnlinkFromSystemUserAsync(long telegramUserId)
    {
        using var activity = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Service"] = "TelegramUserService",
            ["Operation"] = "UnlinkFromSystemUserAsync",
            ["TelegramUserId"] = telegramUserId
        });

        var telegramUser = await _context.TelegramUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId);

        if (telegramUser != null && telegramUser.SystemUserId.HasValue)
        {
            var oldSystemUserId = telegramUser.SystemUserId.Value;
            telegramUser.SystemUserId = null;
            telegramUser.LastActivity = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Telegram user {TelegramUserId} unlinked from system user {SystemUserId}", 
                telegramUserId, oldSystemUserId);
        }
        else
        {
            _logger.LogWarning(
                "Attempted to unlink non-existent user {TelegramUserId} or user without system link", 
                telegramUserId);
        }
    }
}