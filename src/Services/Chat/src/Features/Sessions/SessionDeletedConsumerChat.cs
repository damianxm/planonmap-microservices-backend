using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Contracts.Events.Session;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Chat.Features.Sessions;

public sealed class SessionDeletedConsumerChat(ChatDbContext db, ILogger<SessionDeletedConsumerChat> logger)
    : IConsumer<SessionDeletedEvent>
{
    public async Task Consume(ConsumeContext<SessionDeletedEvent> context)
    {
        var id = context.Message.SessionId;

        var deleted = await db.ChatMessages
            .Where(m => m.SessionId == id)
            .ExecuteDeleteAsync(context.CancellationToken);

        await db.ChatSessions
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(context.CancellationToken);

        logger.LogInformation("Chat cleanup for session {SessionId}: removed {MessageCount} messages", id, deleted);
    }
}
