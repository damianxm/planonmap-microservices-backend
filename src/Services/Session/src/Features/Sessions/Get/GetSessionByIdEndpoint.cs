using InfoMap.Shared.API.Endpoints;
using Microsoft.EntityFrameworkCore;
using Session.Shared.Application.Common;
using Session.Shared.Infrastructure;

namespace Session.Features.Sessions.Get;

public static class GetSessionByIdEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("sessions/{id:guid}", Handler)
                .WithName("GetSessionById")
                .WithSummary("Get a session by its ID")
                .AllowAnonymous()
                .Produces<SessionDto>()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }

    public static async Task<IResult> Handler(Guid id, SessionDbContext db, CancellationToken ct)
    {
        var session = await db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SessionDto(s.Id, s.Code, s.Name, s.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return session is null
            ? Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound)
            : Results.Ok(session);
    }
}
