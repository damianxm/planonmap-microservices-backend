using InfoMap.Shared.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddServiceDiscovery();

builder.ConfigureCors();

builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddServiceDiscoveryDestinationResolver();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
});

builder.ConfigureRateLimiting();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("GeneralCors");
app.UseRateLimiter();

app.UseSecureHeaders();

app.MapReverseProxy()
   .RequireRateLimiting("perIpPolicy");

app.MapDefaultEndpoints();



app.Run();
