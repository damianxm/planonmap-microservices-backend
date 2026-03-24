using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Session.Features.Sessions.Create;
using Session.Shared.Domain.Entities;
using Session.Shared.Infrastructure;

namespace Session.UnitTests;

public class SessionCodeGeneratorTests : IAsyncLifetime
{
    private const string ValidAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int ExpectedLength = 4;

    private readonly SqliteConnection _connection;
    private readonly SessionDbContext _db;

    public SessionCodeGeneratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SessionDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SessionDbContext(options);
    }

    public async ValueTask InitializeAsync() => await _db.Database.EnsureCreatedAsync();

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        _connection.Dispose();
    }

    [Fact]
    public async Task GenerateUniqueAsync_ReturnsCodeWithCorrectFormat()
    {
        var code = await SessionCodeGenerator.GenerateUniqueAsync(_db, CancellationToken.None);

        Assert.Equal(ExpectedLength, code.Length);
        Assert.All(code, c => Assert.Contains(c, ValidAlphabet));
    }

    [Fact]
    public async Task GenerateUniqueAsync_WhenCodeAlreadyExists_ReturnsNewUniqueCode()
    {
        const string existingCode = "AAAA";
        _db.Sessions.Add(new SessionInfo
        {
            Id = Guid.NewGuid(),
            Code = existingCode,
            Name = "Existing",
            CreatedById = "user-1",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var code = await SessionCodeGenerator.GenerateUniqueAsync(_db, CancellationToken.None);

        Assert.NotEqual(existingCode, code);
        Assert.Equal(ExpectedLength, code.Length);
    }
}
