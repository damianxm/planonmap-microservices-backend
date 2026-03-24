using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Infrastructure;
using System.Security.Claims;

namespace Session.Features.Sessions.Access;

public static class LeaveSessionEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("sessions/{sessionId:guid}/participants", Handler)
                .WithName("LeaveSession")
                .WithSummary("Leave a session")
                .RequireAuthorization()
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        ClaimsPrincipal user,
        SessionDbContext db,
        CancellationToken ct)
    {
        var sessionExists = await db.Sessions
            .AsNoTracking()
            .AnyAsync(s => s.Id == sessionId, ct);

        if (!sessionExists)
            return Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound);

        var userId = UserClaims.GetId(user);

        var participant = await db.Participants
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId, ct);

        if (participant is not null)
        {
            db.Participants.Remove(participant);
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }
}
