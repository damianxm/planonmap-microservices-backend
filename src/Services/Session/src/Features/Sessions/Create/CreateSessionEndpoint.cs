using InfoMap.Shared.API.Contracts.Events.Session;
using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Session.Features.Sessions.Clean;
using Session.Shared.Application.Common;
using Session.Shared.Domain.Entities;
using Session.Shared.Infrastructure;
using System.Security.Claims;

namespace Session.Features.Sessions.Create;

public static class CreateSessionEndpoint
{
    public record CreateSessionRequest(string Name, string? DisplayName = null);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("sessions", Handler)
                .WithName("CreateSession")
                .WithSummary("Create a new session")
                .RequireAuthorization()
                .Produces<SessionDto>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<IResult> Handler(
        CreateSessionRequest request,
        ClaimsPrincipal user,
        SessionDbContext db,
        IPublishEndpoint publishEndpoint,
        SessionCleanup cleanupService,
        IIdentityService identityService,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.Problem("Session name cannot be empty.", null, StatusCodes.Status400BadRequest);

        var userId = UserClaims.GetId(user);
        var displayNameFromRequest = UserClaims.NormalizeDisplayName(request.DisplayName);
        var displayName = displayNameFromRequest ?? UserClaims.GetName(user);

        var sessionName = request.Name.Trim();
        var session = new SessionInfo
        {
            Id = Guid.NewGuid(),
            Code = await SessionCodeGenerator.GenerateUniqueAsync(db, ct),
            Name = sessionName[..Math.Min(sessionName.Length, UserClaims.DisplayNameMaxLength)],
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        db.Sessions.Add(session);

        db.Participants.Add(new SessionParticipant
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            UserId = userId,
            DisplayName = displayName,
            JoinedAt = session.CreatedAt
        });

        await db.SaveChangesAsync(ct);

        var totalSessions = await db.Sessions.CountAsync(ct);
        await cleanupService.TryRunAsync(session.Id, totalSessions, ct);

        if (displayNameFromRequest is not null)
            await identityService.SignInAsync(userId, displayNameFromRequest, ct);

        await publishEndpoint.Publish(
            new SessionCreatedEvent(session.Id, session.Name, session.CreatedById, session.CreatedAt),
            ct);

        return Results.Created($"/api/v1/sessions/{session.Id}", new SessionDto(session.Id, session.Code, session.Name, session.CreatedAt));
    }
}
