using InfoMap.Shared.API.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InfoMap.Shared.Infrastructure.Auth;

internal sealed class CookieIdentityService(IHttpContextAccessor httpContextAccessor) : IIdentityService
{
    public async Task<ClaimsPrincipal> SignInAsync(string userId, string displayName, CancellationToken ct = default)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, displayName),
        ],
        CookieAuthenticationDefaults.AuthenticationScheme));

        await httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return principal;
    }
}

internal static class CookieIdentityServiceExtensions
{
    private const string CookieName = "infomap.session";

    internal static IServiceCollection AddCookieIdentityService(this IServiceCollection services)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(2);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IIdentityService, CookieIdentityService>();

        return services;
    }
}
