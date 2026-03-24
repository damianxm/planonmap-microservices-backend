using InfoMap.Shared.Infrastructure;
using MapItems.Features.Markers.Create;
using MapItems.Features.Markers.Hub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MapItems.Shared.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<MarkerRateLimitOptions>(
            builder.Configuration.GetSection("MapItems:RateLimit"));

        builder.AddRedisClient("redis");
        builder.Services.AddSingleton<IRateLimiter, RedisDefaultRateLimiter>();
        builder.Services.AddValidation();

        builder.Services.AddScoped<MapItemsSeeder>();

        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });

    }

    public static async Task UseInfrastructureAsync(this WebApplication app)
    {
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MapItemsDbContext>();
            await dbContext.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<MapItemsSeeder>();
            await seeder.SeedAsync();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHub<MapItemsHub>("/hubs/mapitems");
        app.MapGet("/", () => "MapItems Service");
    }
}
