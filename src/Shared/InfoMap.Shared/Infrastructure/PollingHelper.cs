namespace InfoMap.Shared.Infrastructure;

public static class PollingHelper
{
    public static async Task<T?> WaitForAsync<T>(
        Func<Task<T?>> fetch,
        CancellationToken ct = default,
        int maxAttempts = 10,
        int delayMs = 200) where T : class
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var result = await fetch();
            if (result is not null) return result;
            await Task.Delay(delayMs, ct);
        }
        return null;
    }
}
