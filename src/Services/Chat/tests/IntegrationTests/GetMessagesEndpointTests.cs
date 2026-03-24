using System.Net;
using System.Net.Http.Json;
using Chat.Features.Messages;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Chat.IntegrationTests;

public class GetMessagesEndpointTests : IClassFixture<ChatWebAppFactory>, IAsyncLifetime
{
    private readonly ChatWebAppFactory _factory;
    private readonly HttpClient _client;

    public GetMessagesEndpointTests(ChatWebAppFactory factory)
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

    private async Task SeedMessagesAsync(Guid sessionId, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        for (var i = 0; i < count; i++)
        {
            db.ChatMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                SenderId = "user-1",
                SenderName = "Alice",
                Content = $"Message {i}",
                CreatedAt = DateTime.UtcNow.AddSeconds(-count + i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMessages_ReturnsMessagesInChronologicalOrder()
    {
        var session = await SeedSessionAsync();
        await SeedMessagesAsync(session.Id, 3);

        var response = await _client.GetAsync($"/api/v1/chat/{session.Id}/messages", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<MessagesPageDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(page);
        Assert.Equal(3, page.Items.Count);
        for (var i = 1; i < page.Items.Count; i++)
            Assert.True(page.Items[i].CreatedAt >= page.Items[i - 1].CreatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task GetMessages_InvalidCount_Returns400(int count)
    {
        var session = await SeedSessionAsync();

        var response = await _client.GetAsync($"/api/v1/chat/{session.Id}/messages?count={count}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsOnlyMessagesForRequestedSession()
    {
        var sessionA = await SeedSessionAsync();
        var sessionB = await SeedSessionAsync();
        await SeedMessagesAsync(sessionA.Id, 2);
        await SeedMessagesAsync(sessionB.Id, 3);

        var response = await _client.GetAsync($"/api/v1/chat/{sessionA.Id}/messages", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<MessagesPageDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(page);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, m => Assert.Equal(sessionA.Id, m.SessionId));
    }
}
