using InfoMap.Shared.API.Contracts.Events.Session;
using MapItems.Shared.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MapItems.Features.Consumers;

public sealed class SessionDeletedConsumerMapItems(MapItemsDbContext db, ILogger<SessionDeletedConsumerMapItems> logger) : IConsumer<SessionDeletedEvent>
{
    public async Task Consume(ConsumeContext<SessionDeletedEvent> context)
    {
        var id = context.Message.SessionId;

        var deleted = await db.MapMarkerItems
            .Where(m => m.SessionId == id)
            .ExecuteDeleteAsync(context.CancellationToken);

        await db.MapSessions
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(context.CancellationToken);

        logger.LogInformation("MapItems cleanup for session {SessionId}: removed {MarkerCount} markers", id, deleted);
    }
}
