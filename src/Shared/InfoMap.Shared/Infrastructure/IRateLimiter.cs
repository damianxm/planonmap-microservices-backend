namespace InfoMap.Shared.Infrastructure;

public interface IRateLimiter
{
    Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
    Task<int> TrackViolationAsync(string userId);
}
