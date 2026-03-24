using InfoMap.Shared.API.Endpoints;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Application.Common;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Get;

public static class GetSessionsEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("sessions", Handler)
                .WithName("GetSessions")
                .WithSummary("List all sessions")
                .AllowAnonymous()
                .Produces<List<SessionDto>>();
        }
    }

    public static async Task<IResult> Handler(SessionDbContext db, CancellationToken ct)
    {
        var sessions = await db.Sessions
            .AsNoTracking()
            .OrderBy(s => s.CreatedAt)
            .Select(s => new SessionDto(s.Id, s.Code, s.Name, s.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(sessions);
    }
}
