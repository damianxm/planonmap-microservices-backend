using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Session.Shared.Application.Common;
using Session.Shared.Infrastructure;

namespace Session.IntegrationTests;

public class CreateSessionTests : IClassFixture<SessionWebAppFactory>, IAsyncLifetime
{
    private readonly SessionWebAppFactory _factory;
    private readonly HttpClient _client;

    public CreateSessionTests(SessionWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CreateSession_ValidName_Returns201WithSessionDto()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sessions", new { Name = "My Session" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal("My Session", dto.Name);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.NotEmpty(dto.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateSession_InvalidName_Returns400(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sessions", new { Name = name }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_AddsCreatorAsParticipant()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/sessions", new { Name = "New Session" }, TestContext.Current.CancellationToken);
        var dto = await response.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();

        Assert.True(await db.Participants.AnyAsync(p => p.SessionId == dto!.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSession_GeneratesUniqueCode()
    {
        var r1 = await _client.PostAsJsonAsync("/api/v1/sessions", new { Name = "Session A" }, TestContext.Current.CancellationToken);
        var r2 = await _client.PostAsJsonAsync("/api/v1/sessions", new { Name = "Session B" }, TestContext.Current.CancellationToken);

        var dto1 = await r1.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);
        var dto2 = await r2.Content.ReadFromJsonAsync<SessionDto>(TestContext.Current.CancellationToken);

        Assert.NotEqual(dto1!.Code, dto2!.Code);
    }
}
