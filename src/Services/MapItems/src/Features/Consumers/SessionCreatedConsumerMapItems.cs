using InfoMap.Shared.API.Contracts.Events.Session;
using MapItems.Shared.Domain.Entities;
using MapItems.Shared.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MapItems.Features.Consumers;

public sealed class SessionCreatedConsumerMapItems(MapItemsDbContext db) : IConsumer<SessionCreatedEvent>
{
    public async Task Consume(ConsumeContext<SessionCreatedEvent> context)
    {
        var msg = context.Message;

        var exists = await db.MapSessions.AnyAsync(s => s.Id == msg.SessionId, context.CancellationToken);
        if (exists) return;

        db.MapSessions.Add(new MapSession
        {
            Id = msg.SessionId,
            Name = msg.Name,
            CreatedById = msg.CreatedById,
            CreatedAt = msg.CreatedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
