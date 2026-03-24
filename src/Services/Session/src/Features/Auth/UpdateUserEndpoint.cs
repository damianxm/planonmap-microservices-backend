using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using System.Security.Claims;

namespace Session.Features.Auth;

public static class UpdateUserEndpoint
{
    public record UpdateUserRequest(string? DisplayName);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("auth/user", Handler)
                .WithName("UpdateUser")
                .WithSummary("Update display name")
                .RequireAuthorization()
                .Produces<UserDto>()
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<IResult> Handler(
        UpdateUserRequest request,
        ClaimsPrincipal user,
        IIdentityService identityService)
    {
        var displayName = UserClaims.NormalizeDisplayName(request.DisplayName);
        if (displayName is null)
            return Results.Problem("Display name cannot be empty.", statusCode: StatusCodes.Status400BadRequest);

        var userId = UserClaims.GetId(user);

        await identityService.SignInAsync(userId, displayName);

        return Results.Ok(new UserDto(userId, displayName));
    }
}
