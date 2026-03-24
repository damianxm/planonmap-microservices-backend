using Chat.Shared.Application.Common;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Endpoints;
using Microsoft.EntityFrameworkCore;

namespace Chat.Features.Messages;

public static class GetMessagesEndpoint
{
    private static readonly Func<ChatDbContext, Guid, DateTime, int, IAsyncEnumerable<ChatMessage>> GetPageQuery =
        EF.CompileAsyncQuery(
            (ChatDbContext db, Guid sessionId, DateTime before, int count) =>
                db.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.SessionId == sessionId && m.CreatedAt < before)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(count));

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("chat/{sessionId}/messages", Handler)
                .WithName("GetChatMessages")
                .WithSummary("Get paginated chat messages for a session")
                .AllowAnonymous()
                .Produces<MessagesPageDto>()
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        ChatDbContext db,
        int count = 50,
        DateTime? before = null,
        CancellationToken ct = default)
    {
        if (count is < 1 or > 200)
            return Results.Problem("'count' must be between 1 and 200.", statusCode: StatusCodes.Status400BadRequest);

        var page = await LoadPageAsync(db, sessionId, count, before, ct);
        return Results.Ok(page);
    }

    internal static async Task<MessagesPageDto> LoadPageAsync(
        ChatDbContext db,
        Guid sessionId,
        int count,
        DateTime? before,
        CancellationToken ct)
    {
        var cursor = before ?? DateTime.UtcNow.AddSeconds(1);
        var safeCount = Math.Clamp(count, 1, 200);

        var messages = new List<ChatMessage>(safeCount);
        await foreach (var msg in GetPageQuery(db, sessionId, cursor, safeCount).WithCancellation(ct))
            messages.Add(msg);

        messages.Reverse();

        DateTime? nextCursor = messages.Count == safeCount ? messages[0].CreatedAt : null;

        return new MessagesPageDto(
            messages.Select(m => new ChatMessageDto(m.Id, m.SenderId, m.SenderName, m.SessionId, m.Content, m.CreatedAt)).ToList(),
            nextCursor);
    }
}
