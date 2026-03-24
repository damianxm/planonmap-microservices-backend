using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Application.Common;
using Session.Shared.Domain.Entities;
using Session.Shared.Infrastructure;
using System.Security.Claims;

namespace Session.Features.Sessions.Access;

public static class JoinSessionEndpoint
{
    public record JoinSessionRequest(string? DisplayName = null);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("sessions/{sessionId:guid}/participants", Handler)
                .WithName("JoinSession")
                .WithSummary("Join a session as participant")
                .RequireAuthorization()
                .Produces<List<ParticipantDto>>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        JoinSessionRequest request,
        ClaimsPrincipal user,
        SessionDbContext db,
        IIdentityService identityService,
        CancellationToken ct)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
            return Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound);

        var userId = UserClaims.GetId(user);
        var displayNameFromRequest = UserClaims.NormalizeDisplayName(request.DisplayName);
        var displayName = displayNameFromRequest ?? UserClaims.GetName(user);

        var existing = await db.Participants
            .FirstOrDefaultAsync(p => p.SessionId == sessionId && p.UserId == userId, ct);

        if (existing is not null)
        {
            existing.DisplayName = displayName;
        }
        else
        {
            db.Participants.Add(new SessionParticipant
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                DisplayName = displayName,
                JoinedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);

        if (displayNameFromRequest is not null)
            await identityService.SignInAsync(userId, displayNameFromRequest, ct);

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
