using InfoMap.Shared.API.Contracts.Events.Session;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Session.Features.Sessions.Clean;
using Session.Shared.Domain.Entities;
using Session.Shared.Infrastructure;

namespace Session.UnitTests;

public class SessionCleanupTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly SessionDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly SessionCleanup _sut;

    public SessionCleanupTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SessionDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SessionDbContext(options);
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _sut = new SessionCleanup(_db, _publishEndpoint, NullLogger<SessionCleanup>.Instance);
    }

    public async ValueTask InitializeAsync() => await _db.Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        _connection.Dispose();
    }

    private SessionInfo MakeSession(DateTime createdAt, string code) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = "Test",
        CreatedById = "user-1",
        CreatedAt = createdAt
    };

    [Theory]
    [InlineData(1)]
    [InlineData(21)]
    public async Task TryRunAsync_TotalNotMultipleOf20_DoesNotRun(int totalSessions)
    {
        await _sut.TryRunAsync(Guid.NewGuid(), totalSessions, CancellationToken.None);

        await _publishEndpoint.DidNotReceiveWithAnyArgs()
            .Publish<SessionDeletedEvent>(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TryRunAsync_MultipleOf20_DeletesExpiredSessions()
    {
        var expired = MakeSession(DateTime.UtcNow.AddDays(-3), "EXPI");
        _db.Sessions.Add(expired);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.TryRunAsync(Guid.NewGuid(), 20, CancellationToken.None);

        Assert.False(await _db.Sessions.AnyAsync(s => s.Id == expired.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryRunAsync_MultipleOf20_KeepsActiveSessions()
    {
        var active = MakeSession(DateTime.UtcNow, "ACTV");
        _db.Sessions.Add(active);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.TryRunAsync(Guid.NewGuid(), 20, CancellationToken.None);

        Assert.True(await _db.Sessions.AnyAsync(s => s.Id == active.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryRunAsync_MultipleOf20_PublishesDeletedEventForEachExpiredSession()
    {
        var expired1 = MakeSession(DateTime.UtcNow.AddDays(-3), "EXP1");
        var expired2 = MakeSession(DateTime.UtcNow.AddDays(-5), "EXP2");
        _db.Sessions.AddRange(expired1, expired2);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.TryRunAsync(Guid.NewGuid(), 20, CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<SessionDeletedEvent>(e => e.SessionId == expired1.Id),
            Arg.Any<CancellationToken>());

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<SessionDeletedEvent>(e => e.SessionId == expired2.Id),
            Arg.Any<CancellationToken>());
    }

}
