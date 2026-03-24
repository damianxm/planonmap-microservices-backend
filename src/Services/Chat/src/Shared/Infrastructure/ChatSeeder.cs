using Microsoft.EntityFrameworkCore;

namespace Chat.Shared.Infrastructure;

public sealed class ChatSeeder(ChatDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await dbContext.Database.CanConnectAsync(ct)) return;
    }
}
