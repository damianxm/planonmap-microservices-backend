using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.Infrastructure.Auth;
using InfoMap.Shared.Infrastructure.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace InfoMap.Shared.Infrastructure.Extensions;

public static class ServiceBuilderExtension
{
    public const string DataProtectionAppName = "InfoMap";

    public static void AddDefaultInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.Services.AddEndpoints();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<OperationCancelledExceptionHandler>();

        var dp = builder.Services.AddDataProtection()
            .SetApplicationName(DataProtectionAppName);

        var redisConnection = builder.Configuration.GetConnectionString("redis");
        if (redisConnection is not null)
            dp.PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redisConnection), "DataProtection-Keys");

        builder.Services.AddCookieIdentityService();

        builder.Services.AddAuthorization();
    }

    public static void UseDefaultInfrastructure(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        var v1 = app.MapGroup("/api/v1");
        app.MapEndpoints(v1);
    }

    public static void ConfigureMassTransit(this IHostApplicationBuilder builder, Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        //builder.AddRabbitMQClient("rabbitmq");
        builder.Services.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();

            configureConsumers?.Invoke(configurator);

            configurator.UsingRabbitMq((context, cfg) =>
            {
                var connectionString = builder.Configuration.GetConnectionString("rabbitmq");

                cfg.Host(connectionString);

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}