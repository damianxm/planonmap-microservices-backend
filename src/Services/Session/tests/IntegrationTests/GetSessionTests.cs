using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Session.Shared.Application.Common;
using Session.Shared.Domain.Entities;
using Session.Shared.Infrastructure;

namespace Session.IntegrationTests;

public class GetSessionTests : IClassFixture<SessionWebAppFactory>, IAsyncLifetime
{
    private readonly SessionWebAppFactory _factory;
    private readonly HttpClient _client;

    public GetSessionTests(SessionWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Sessions.RemoveRange(db.Sessions);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<SessionInfo> SeedSessionAsync(string name, string code)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();

        var session = new SessionInfo
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    // --- GET /sessions ---

    [Fact]
    public async Task GetSessions_ReturnsAllSessions()
    {
        await SeedSessionAsync("Alpha", "ALPH");
        await SeedSessionAsync("Beta", "BETA");

        var response = await _client.GetAsync("/api/v1/sessions", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var list = await response.Content.ReadFromJsonAsync<List<SessionDto>>(TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
    }

    // --- GET /sessions/{id} ---

    [Fact]
    public async Task GetSessionById_WhenExists_ReturnsSession()
    {
        var seeded = await SeedSessionAsync("Delta", "DELT");

        var response = await _client.GetAsync($"/api/v1/sessions/{seeded.Id}", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal(seeded.Id, dto.Id);
        Assert.Equal("Delta", dto.Name);
    }

    [Fact]
    public async Task GetSessionById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/sessions/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- GET /sessions/by-code/{code} ---

    [Fact]
    public async Task GetSessionByCode_WhenExists_ReturnsSession()
    {
        await SeedSessionAsync("Echo", "ECHO");

        var response = await _client.GetAsync("/api/v1/sessions/by-code/ECHO", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal("ECHO", dto.Code);
    }

    [Fact]
    public async Task GetSessionByCode_IsCaseInsensitive()
    {
        await SeedSessionAsync("Foxtrot", "FOXT");

        var response = await _client.GetAsync("/api/v1/sessions/by-code/foxt", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

}
