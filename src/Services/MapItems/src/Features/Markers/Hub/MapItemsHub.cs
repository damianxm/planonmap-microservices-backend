using InfoMap.Shared.API.Identity;
using InfoMap.Shared.Infrastructure;
using MapItems.Features.Markers.Create;
using MapItems.Shared.Application.Contracts;
using MapItems.Shared.Domain.Common;
using MapItems.Shared.Domain.Entities;
using MapItems.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MapItems.Features.Markers.Hub;

[Authorize]
public sealed class MapItemsHub(
    MapItemsDbContext db,
    IRateLimiter rateLimiter,
    IOptions<MarkerRateLimitOptions> rateLimitOptions,
    ILogger<MapItemsHub> logger)
    : Hub<IMapItemsHubClient>
{
    private readonly MarkerRateLimitOptions _options = rateLimitOptions.Value;

    public override async Task OnConnectedAsync()
    {
        logger.LogDebug("Connected: {ConnectionId} ({Name})", Context.ConnectionId, UserClaims.GetName(Context.User!));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
            logger.LogWarning(exception, "Disconnected with error: {ConnectionId}", Context.ConnectionId);
        else
            logger.LogDebug("Disconnected: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(Guid sessionId)
    {
        var ct = Context.ConnectionAborted;

        var session = await PollingHelper.WaitForAsync(
            () => db.MapSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sessionId, ct),
            ct);

        if (session is null)
        {
            logger.LogWarning("Session {SessionId} not found for {ConnectionId}", sessionId, Context.ConnectionId);
            throw new HubException("Session not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString(), ct);

        var markers = await db.MapMarkerItems
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .Select(m => new MarkerDto(m.Id, m.Name, m.Description, m.Latitude, m.Longitude, m.SessionId))
            .ToListAsync(ct);

        await Clients.Caller.LoadMarkers(markers);

        logger.LogInformation("{Name} ({ConnectionId}) joined map session {SessionId}",
            UserClaims.GetName(Context.User!), Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());
    }

    public async Task AddMarker(Guid sessionId, string name, string? description, float latitude, float longitude)
    {
        var userId = UserClaims.GetId(Context.User!);

        var allowed = await rateLimiter.IsAllowedAsync($"map:marker:{userId}", _options.MarkerLimit, _options.MarkerWindow);
        if (!allowed)
        {
            logger.LogWarning("Marker rate limit exceeded for user {UserId} ({ConnectionId})", userId, Context.ConnectionId);
            throw new HubException("You're adding markers too fast. Please slow down.");
        }

        var (nameValid, nameError) = MarkerContentRules.ValidateName(name);
        if (!nameValid)
        {
            logger.LogWarning("Invalid marker name from {UserId} ({ConnectionId}): {Error}", userId, Context.ConnectionId, nameError);
            throw new HubException(nameError!);
        }

        var (descValid, descError) = MarkerContentRules.ValidateDescription(description);
        if (!descValid)
        {
            logger.LogWarning("Invalid marker description from {UserId} ({ConnectionId}): {Error}", userId, Context.ConnectionId, descError);
            throw new HubException(descError!);
        }

        var sessionExists = await db.MapSessions.AnyAsync(s => s.Id == sessionId, Context.ConnectionAborted);
        if (!sessionExists)
        {
            logger.LogWarning("AddMarker to unknown session {SessionId} by {UserId} ({ConnectionId})", sessionId, userId, Context.ConnectionId);
            throw new HubException("Session not found.");
        }

        var marker = new MapMarkerItems
        {
            Name = name,
            Description = description,
            Latitude = latitude,
            Longitude = longitude,
            SessionId = sessionId
        };

        db.MapMarkerItems.Add(marker);
        await db.SaveChangesAsync(Context.ConnectionAborted);

        var dto = new MarkerDto(marker.Id, marker.Name, marker.Description, marker.Latitude, marker.Longitude, marker.SessionId);
        await Clients.Group(sessionId.ToString()).ReceiveMarker(dto);
    }
}
