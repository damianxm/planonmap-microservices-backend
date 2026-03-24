using InfoMap.Shared.API.Endpoints;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Application.Common;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Get;

public static class GetSessionByCodeEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("sessions/by-code/{code}", Handler)
                .WithName("GetSessionByCode")
                .WithSummary("Find a session by its short code")
                .AllowAnonymous()
                .Produces<SessionDto>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public static async Task<IResult> Handler(string code, SessionDbContext db, CancellationToken ct)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .Where(s => s.Code == code.ToUpperInvariant())
            .Select(s => new SessionDto(s.Id, s.Code, s.Name, s.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return session is null
            ? Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(session);
    }
}
