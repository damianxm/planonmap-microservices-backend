using InfoMap.Shared.API.Endpoints;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Application.Common;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Get;

public static class GetSessionParticipantsEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("sessions/{sessionId:guid}/participants", Handler)
                .WithName("GetParticipants")
                .WithSummary("Get participants of a session")
                .AllowAnonymous()
                .Produces<List<ParticipantDto>>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        SessionDbContext db,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound);

        var participants = await db.Participants
            .AsNoTracking()
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.JoinedAt)
            .Select(p => new ParticipantDto(
                p.UserId,
                p.DisplayName,
                p.JoinedAt,
                p.UserId == session.CreatedById))
            .ToListAsync(ct);

        return Results.Ok(participants);
    }
}
