
using System.Threading.RateLimiting;

namespace InfoMap.Shared.Extensions;

public static class ServiceBuilderExtension
{
    public static void UseSecureHeaders(this WebApplication app)
    {
        app.Use(async (ctx, next) =>
        {
            var headers = ctx.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            headers["Content-Security-Policy"] = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'; upgrade-insecure-requests;";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), interest-cohort=()";
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            await next();
        });
    }

    public static void ConfigureCors(this IHostApplicationBuilder builder)
    {
        var allowedOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("No CORS origins configured!");
        }

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("GeneralCors", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });
    }

    public static void ConfigureRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("perIpPolicy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromSeconds(10),
                        SegmentsPerWindow = 5,
                        QueueLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
    }
}