using StackExchange.Redis;

namespace InfoMap.Shared.Infrastructure;

public sealed class RedisDefaultRateLimiter(IConnectionMultiplexer redis) : IRateLimiter
{
    public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
    {
        var db = redis.GetDatabase();
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / (long)window.TotalMilliseconds;
        var bucketKey = $"{key}:{bucket}";

        var count = await db.StringIncrementAsync(bucketKey);
        if (count == 1)
            await db.KeyExpireAsync(bucketKey, window);

        return count <= limit;
    }

    public async Task<int> TrackViolationAsync(string userId)
    {
        var key = $"spam:violations:{userId}";
        var db = redis.GetDatabase();
        var count = (int)await db.StringIncrementAsync(key);
        if (count == 1)
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));
        return count;
    }
}
