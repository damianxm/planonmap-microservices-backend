using System.Security.Claims;

namespace InfoMap.Shared.API.Identity;

public interface IIdentityService
{
    Task<ClaimsPrincipal> SignInAsync(string userId, string displayName, CancellationToken ct = default);
}
