using Microsoft.EntityFrameworkCore;
using Session.Shared.Domain.Entities;

namespace Session.Shared.Infrastructure;

public sealed class SessionSeeder(SessionDbContext dbContext)
{
    public static readonly Guid GeneralSessionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await dbContext.Database.CanConnectAsync(ct)) return;
        if (await dbContext.Sessions.AnyAsync(ct)) return;

        dbContext.Sessions.Add(new SessionInfo
        {
            Id = GeneralSessionId,
            Name = "General",
            CreatedById = "system",
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
    }
}
