using System.Net;
using System.Net.Http.Json;
using Chat.Shared.Application.Common;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.IntegrationTests;

public class SendMessageEndpointTests : IClassFixture<ChatWebAppFactory>, IAsyncLifetime
{
    private readonly ChatWebAppFactory _factory;
    private readonly HttpClient _client;

    public SendMessageEndpointTests(ChatWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.ChatMessages.RemoveRange(db.ChatMessages);
        db.ChatSessions.RemoveRange(db.ChatSessions);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<ChatSession> SeedSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task SendMessage_ValidRequest_Returns201WithMessageDto()
    {
        var session = await SeedSessionAsync();

        var response = await _client.PostAsJsonAsync($"/api/v1/chat/{session.Id}/messages", new { Content = "Hello!" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<ChatMessageDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(dto);
        Assert.Equal("Hello!", dto.Content);
        Assert.Equal(session.Id, dto.SessionId);
    }

    [Theory]
    [InlineData(0)]  
    [InlineData(501)]
    public async Task SendMessage_InvalidContent_Returns400(int contentLength)
    {
        var session = await SeedSessionAsync();
        var content = new string('x', contentLength);

        var response = await _client.PostAsJsonAsync($"/api/v1/chat/{session.Id}/messages", new { Content = content }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
