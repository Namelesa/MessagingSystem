using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MessagingSystem.Services.Messaging.Infrastructure.Caching;

public class CacheService(IDistributedCache cache, ILogger<CacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        logger.LogInformation("Trying to GET from cache with key: {CacheKey}", key);
        var value = await cache.GetStringAsync(key);

        if (value == null)
        {
            logger.LogInformation("Cache MISS for key: {CacheKey}", key);
            return default;
        }

        logger.LogInformation("Cache HIT for key: {CacheKey}", key);
        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var json = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });

        logger.LogInformation("SET cache key: {CacheKey} with TTL: {TTL}", key, ttl);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
        logger.LogInformation("REMOVED cache key: {CacheKey}", key);
    }
}