using Chat.Features.Messages.Hub;
using Chat.Features.Messages.Send;
using InfoMap.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Chat.Shared.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ChatRateLimitOptions>(
            builder.Configuration.GetSection("Chat:RateLimit"));

        builder.AddRedisClient("redis");
        builder.Services.AddSingleton<IRateLimiter, RedisDefaultRateLimiter>();
        builder.Services.AddScoped<SendMessage>();
        builder.Services.AddValidation();

        builder.Services.AddScoped<ChatSeeder>();

        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });

    }

    public static async Task UseInfrastructureAsync(this WebApplication app)
    {
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            await db.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<ChatSeeder>();
            await seeder.SeedAsync();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<ChatHub>("/hubs/chat");
    }
}
