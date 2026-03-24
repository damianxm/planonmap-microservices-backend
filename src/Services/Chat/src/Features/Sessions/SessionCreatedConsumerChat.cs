using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Contracts.Events.Session;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Chat.Features.Sessions;

public sealed class SessionCreatedConsumerChat(ChatDbContext db) : IConsumer<SessionCreatedEvent>
{
    public async Task Consume(ConsumeContext<SessionCreatedEvent> context)
    {
        var msg = context.Message;

        var exists = await db.ChatSessions.AnyAsync(s => s.Id == msg.SessionId, context.CancellationToken);
        if (exists) return;

        db.ChatSessions.Add(new ChatSession
        {
            Id = msg.SessionId,
            Name = msg.Name,
            CreatedById = msg.CreatedById,
            CreatedAt = msg.CreatedAt
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
