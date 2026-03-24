using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using System.Security.Claims;

namespace Session.Features.Auth;

public static class GetUserEndpoint
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("auth/user", Handler)
                .WithName("GetUser")
                .WithSummary("Get current anonymous identity")
                .RequireAuthorization()
                .Produces<UserDto>();
        }
    }

    public static IResult Handler(ClaimsPrincipal user) =>
        Results.Ok(new UserDto(
            UserClaims.GetId(user),
            UserClaims.GetName(user)));
}
