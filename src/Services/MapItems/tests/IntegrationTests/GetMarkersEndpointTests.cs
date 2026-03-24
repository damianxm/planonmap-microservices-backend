using System.Net.Http.Json;
using MapItems.Shared.Application.Contracts;
using MapItems.Shared.Domain.Entities;
using MapItems.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace MapItems.IntegrationTests;

public class GetMarkersEndpointTests : IClassFixture<MapItemsWebAppFactory>, IAsyncLifetime
{
    private readonly MapItemsWebAppFactory _factory;
    private readonly HttpClient _client;

    public GetMarkersEndpointTests(MapItemsWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MapItemsDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.MapMarkerItems.RemoveRange(db.MapMarkerItems);
        db.MapSessions.RemoveRange(db.MapSessions);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<MapSession> SeedSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MapItemsDbContext>();
        var session = new MapSession { Id = Guid.NewGuid() };
        db.MapSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    private async Task SeedMarkerAsync(Guid sessionId, string name)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MapItemsDbContext>();
        db.MapMarkerItems.Add(new MapMarkerItems
        {
            SessionId = sessionId,
            Name = name,
            Description = null,
            Latitude = 52.2f,
            Longitude = 21.0f
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMarkers_ReturnsOnlyMarkersForRequestedSession()
    {
        var sessionA = await SeedSessionAsync();
        var sessionB = await SeedSessionAsync();
        await SeedMarkerAsync(sessionA.Id, "A-Marker");
        await SeedMarkerAsync(sessionB.Id, "B-Marker 1");
        await SeedMarkerAsync(sessionB.Id, "B-Marker 2");

        var response = await _client.GetAsync($"/api/v1/markers/{sessionA.Id}", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var markers = await response.Content.ReadFromJsonAsync<List<MarkerDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(markers);
        Assert.Single(markers);
        Assert.Equal("A-Marker", markers[0].Name);
    }

}
