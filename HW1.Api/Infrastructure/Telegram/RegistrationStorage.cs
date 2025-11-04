using System.Text.Json;
using HW1.Api.WebAPI.TelegramBot.Commands;
using Microsoft.Extensions.Caching.Distributed;

namespace HW1.Api.Infrastructure.Telegram;

public interface IRegistrationStorage
{
    Task<UserRegistrationData?> GetRegistrationStateAsync(long telegramUserId);
    Task SetRegistrationStateAsync(long telegramUserId, UserRegistrationData state);
    Task RemoveRegistrationStateAsync(long telegramUserId);
    Task<bool> IsUserInRegistrationAsync(long telegramUserId);
}

public class RegistrationStorage : IRegistrationStorage
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RegistrationStorage> _logger;

    public RegistrationStorage(IDistributedCache cache, ILogger<RegistrationStorage> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<UserRegistrationData?> GetRegistrationStateAsync(long telegramUserId)
    {
        try
        {
            var key = GetCacheKey(telegramUserId);
            var json = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<UserRegistrationData>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting registration state for user {UserId}", telegramUserId);
            return null;
        }
    }

    public async Task SetRegistrationStateAsync(long telegramUserId, UserRegistrationData state)
    {
        try
        {
            var key = GetCacheKey(telegramUserId);
            var json = JsonSerializer.Serialize(state);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // 30 минут TTL
            };
            
            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting registration state for user {UserId}", telegramUserId);
        }
    }

    public async Task RemoveRegistrationStateAsync(long telegramUserId)
    {
        try
        {
            var key = GetCacheKey(telegramUserId);
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing registration state for user {UserId}", telegramUserId);
        }
    }

    public async Task<bool> IsUserInRegistrationAsync(long telegramUserId)
    {
        try
        {
            var key = GetCacheKey(telegramUserId);
            var json = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking registration state for user {UserId}", telegramUserId);
            return false;
        }
    }

    private static string GetCacheKey(long telegramUserId) => $"registration:{telegramUserId}";
}