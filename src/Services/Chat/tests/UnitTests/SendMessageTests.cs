using Chat.Features.Messages.Hub;
using Chat.Features.Messages.Send;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Contracts.Events.Chat;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Chat.UnitTests;

public class SendMessageTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly ChatDbContext _db;
    private readonly IHubContext<ChatHub, IChatHubClient> _hubContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly SendMessage _sut;

    public SendMessageTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ChatDbContext(options);

        var hubClients = Substitute.For<IHubClients<IChatHubClient>>();
        var hubClient = Substitute.For<IChatHubClient>();
        hubClients.Group(Arg.Any<string>()).Returns(hubClient);

        _hubContext = Substitute.For<IHubContext<ChatHub, IChatHubClient>>();
        _hubContext.Clients.Returns(hubClients);

        _publishEndpoint = Substitute.For<IPublishEndpoint>();

        _sut = new SendMessage(_db, _hubContext, _publishEndpoint);
    }

    public async ValueTask InitializeAsync() => await _db.Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        _connection.Dispose();
    }

    private async Task<ChatSession> AddSessionAsync()
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            Name = "Test Session",
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        };
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    [Fact]
    public async Task SendAsync_SavesMessageToDatabase()
    {
        var session = await AddSessionAsync();
        var dto = new SendMessageDto("user-1", "Alice", session.Id, "Hello!");

        await _sut.SendAsync(dto, TestContext.Current.CancellationToken);

        var saved = await _db.ChatMessages.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Hello!", saved.Content);
        Assert.Equal("user-1", saved.SenderId);
        Assert.Equal("Alice", saved.SenderName);
        Assert.Equal(session.Id, saved.SessionId);
    }

    [Fact]
    public async Task SendAsync_PublishesChatMessageCreatedEvent()
    {
        var session = await AddSessionAsync();
        var dto = new SendMessageDto("user-1", "Alice", session.Id, "Hello!");

        await _sut.SendAsync(dto, TestContext.Current.CancellationToken);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<ChatMessageCreatedEvent>(e =>
                e.Content == "Hello!" &&
                e.SenderId == "user-1" &&
                e.SessionId == session.Id),
            Arg.Any<CancellationToken>());
    }

}
