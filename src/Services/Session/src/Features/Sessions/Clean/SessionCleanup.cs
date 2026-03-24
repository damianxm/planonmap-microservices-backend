using InfoMap.Shared.API.Contracts.Events.Session;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Clean;

public sealed class SessionCleanup(
    SessionDbContext db,
    IPublishEndpoint publishEndpoint,
    ILogger<SessionCleanup> logger)
{
    private const int CleanupEvery = 20;
    private const int CleanUpDayInterval = 2;

    public async Task TryRunAsync(Guid excludeSessionId, int totalSessions, CancellationToken ct)
    {
        if (totalSessions % CleanupEvery != 0) return;

        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(CleanUpDayInterval);

        var expired = await db.Sessions
            .Where(s => s.CreatedAt < cutoff && s.Id != excludeSessionId)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        var ids = expired.Select(s => s.Id).ToList();

        await db.Participants
            .Where(p => ids.Contains(p.SessionId))
            .ExecuteDeleteAsync(ct);

        await db.Sessions
            .Where(s => ids.Contains(s.Id))
            .ExecuteDeleteAsync(ct);

        foreach (var session in expired)
            await publishEndpoint.Publish(new SessionDeletedEvent(session.Id), ct);

        logger.LogInformation("Session cleanup: deleted {Count} expired sessions (triggered at {Total} total)",
            expired.Count, totalSessions);
    }
}
