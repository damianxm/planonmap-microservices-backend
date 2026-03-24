using InfoMap.Shared.API.Endpoints;
using MapItems.Shared.Application.Contracts;
using MapItems.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MapItems.Features.Markers.Get;

public static class GetMarkersEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/markers/{sessionId:guid}", Handler)
                .WithTags("Markers")
                .AllowAnonymous()
                .Produces<List<MarkerDto>>();
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        MapItemsDbContext db,
        CancellationToken ct)
    {
        var markers = await db.MapMarkerItems
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .Select(m => new MarkerDto(m.Id, m.Name, m.Description, m.Latitude, m.Longitude, m.SessionId))
            .ToListAsync(ct);

        return Results.Ok(markers);
    }
}
