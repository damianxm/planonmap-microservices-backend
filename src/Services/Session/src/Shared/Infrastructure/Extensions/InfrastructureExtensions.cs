using InfoMap.Shared.API.Identity;
using Microsoft.EntityFrameworkCore;
using Session.Features.Auth;
using Session.Features.Sessions.Clean;


namespace Session.Shared.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static void AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<SessionSeeder>();
        builder.Services.AddScoped<SessionCleanup>();
    }

    public static async Task UseInfrastructureAsync(this WebApplication app)
    {
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
            await db.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<SessionSeeder>();
            await seeder.SeedAsync();
        }

        app.UseAuthentication();

        app.Use(async (context, next) =>
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false) &&
                !context.WebSockets.IsWebSocketRequest)
            {
                var identityService = context.RequestServices.GetRequiredService<IIdentityService>();
                var id = Guid.NewGuid().ToString();
                context.User = await identityService.SignInAsync(id, $"Guest-{id[..6]}");
            }

            await next();
        });

        app.UseAuthorization();
    }
}
