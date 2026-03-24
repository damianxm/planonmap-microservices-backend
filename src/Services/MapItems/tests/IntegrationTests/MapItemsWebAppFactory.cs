using InfoMap.Shared.Tests;
using MapItems.Shared.Infrastructure;
using Microsoft.AspNetCore.Hosting;

namespace MapItems.IntegrationTests;

public class MapItemsWebAppFactory : IntegrationTestFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.ReplaceDbContext<MapItemsDbContext>(Connection);
            services.UseFakeRateLimiter();
            services.UseTestAuth<TestAuthHandler>();
            services.UseTestMassTransit();
        });
    }
}
