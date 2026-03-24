using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using StackExchange.Redis;

namespace InfoMap.Shared.Tests;

public static class TestServiceCollectionExtensions
{
    public static void ReplaceDbContext<TContext>(this IServiceCollection services, SqliteConnection connection) where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<TContext>>();

        services.AddDbContext<TContext>(o =>
            o.UseSqlite(connection)
             .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddEndpoints(typeof(TContext).Assembly);
    }

    public static void UseTestAuth<THandler>(this IServiceCollection services) where THandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, THandler>(TestAuthHandler.SchemeName, _ => { });
    }

    public static void UseTestMassTransit(this IServiceCollection services)
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType.Assembly.GetName().Name?.StartsWith("MassTransit") == true ||
                d.ImplementationType?.Assembly.GetName().Name?.StartsWith("MassTransit") == true ||
                d.ImplementationInstance?.GetType().Assembly.GetName().Name?.StartsWith("MassTransit") == true)
            .ToList();

        foreach (var d in toRemove) services.Remove(d);

        services.AddMassTransitTestHarness();
    }

    public static void UseFakeRateLimiter(this IServiceCollection services)
    {
        services.RemoveAll<IConnectionMultiplexer>();
        services.RemoveAll<IRateLimiter>();

        services.AddSingleton(_ =>
        {
            var limiter = Substitute.For<IRateLimiter>();
            limiter.IsAllowedAsync(default!, default, default).ReturnsForAnyArgs(true);
            return limiter;
        });
    }
}
