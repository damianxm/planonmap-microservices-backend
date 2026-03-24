using System.Security.Claims;

namespace InfoMap.Shared.API.Identity;

public static class UserClaims
{
    public const int DisplayNameMaxLength = 100;

    public static string GetId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing NameIdentifier claim.");

    public static string GetName(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? "Anonymous";

    public static string? NormalizeDisplayName(string? raw) =>
        raw?.Trim() is { Length: > 0 } s ? s[..Math.Min(s.Length, DisplayNameMaxLength)] : null;
}
