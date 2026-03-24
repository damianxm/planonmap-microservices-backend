using InfoMap.Shared.Tests;
using Microsoft.AspNetCore.Hosting;
using Session.Shared.Infrastructure;

namespace Session.IntegrationTests;

public class SessionWebAppFactory : IntegrationTestFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.ReplaceDbContext<SessionDbContext>(Connection);
            services.UseTestAuth<TestAuthHandler>();
            services.UseTestMassTransit();
        });
    }
}
