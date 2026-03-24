using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using InfoMap.Shared.Infrastructure;
using MapItems.Features.Markers.Hub;
using MapItems.Shared.Application.Contracts;
using MapItems.Shared.Domain.Common;
using MapItems.Shared.Domain.Entities;
using MapItems.Shared.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MapItems.Features.Markers.Create;

public static class CreateMarkerEndpoint
{
    public record Request(
        [property: Required, MaxLength(100)] string Name,
        [property: MaxLength(300)] string? Description,
        float Latitude,
        float Longitude,
        Guid SessionId);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/markers", Handler)
                .WithTags("Markers")
                .RequireAuthorization()
                .Produces<MarkerDto>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status429TooManyRequests);
        }
    }

    public static async Task<IResult> Handler(
        Request request,
        ClaimsPrincipal user,
        MapItemsDbContext db,
        IHubContext<MapItemsHub, IMapItemsHubClient> hub,
        IRateLimiter rateLimiter,
        IOptions<MarkerRateLimitOptions> rateLimitOptions,
        ILogger<Endpoint> logger,
        CancellationToken ct)
    {
        var userId = UserClaims.GetId(user);
        var options = rateLimitOptions.Value;

        var allowed = await rateLimiter.IsAllowedAsync($"map:marker:{userId}", options.MarkerLimit, options.MarkerWindow);
        if (!allowed)
        {
            logger.LogWarning("Marker REST rate limit exceeded for user {UserId}", userId);
            return Results.Problem("You're adding markers too fast.", statusCode: StatusCodes.Status429TooManyRequests);
        }

        var (valid, error) = MarkerContentRules.ValidateName(request.Name);
        if (!valid)
        {
            logger.LogWarning("Invalid marker name from user {UserId}: {Error}", userId, error);
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        }

        var session = await db.MapSessions.FindAsync([request.SessionId], ct);
        if (session is null)
            return Results.Problem("Session not found.", statusCode: StatusCodes.Status404NotFound);

        var marker = new MapMarkerItems
        {
            Name = request.Name,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            SessionId = request.SessionId
        };

        db.MapMarkerItems.Add(marker);
        await db.SaveChangesAsync(ct);

        var dto = new MarkerDto(marker.Id, marker.Name, marker.Description, marker.Latitude, marker.Longitude, marker.SessionId);
        await hub.Clients.Group(request.SessionId.ToString()).ReceiveMarker(dto);

        return Results.Created($"/api/v1/markers/{marker.Id}", dto);
    }
}
