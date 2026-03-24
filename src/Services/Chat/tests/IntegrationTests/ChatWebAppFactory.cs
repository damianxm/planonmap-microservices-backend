using Chat.Shared.Infrastructure;
using InfoMap.Shared.Tests;
using Microsoft.AspNetCore.Hosting;

namespace Chat.IntegrationTests;

public class ChatWebAppFactory : IntegrationTestFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.ReplaceDbContext<ChatDbContext>(Connection);
            services.UseFakeRateLimiter();
            services.UseTestAuth<TestAuthHandler>();
            services.UseTestMassTransit();
        });
    }
}
