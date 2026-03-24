using Chat.Features.Messages.Send;
using Chat.Shared.Application.Common;
using InfoMap.Shared.API.Endpoints;
using InfoMap.Shared.API.Identity;
using InfoMap.Shared.Infrastructure;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static Chat.Features.Messages.Send.SendMessage;

namespace Chat.Features.Messages;

public static class SendMessageEndpoint
{
    public record SendMessageRequest([property: Required, MaxLength(500)] string Content);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("chat/{sessionId}/messages", Handler)
                .WithName("SendChatMessage")
                .WithSummary("Send a message to a session")
                .RequireAuthorization()
                .Produces<ChatMessageDto>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status429TooManyRequests);
        }
    }

    public static async Task<IResult> Handler(
        Guid sessionId,
        SendMessageRequest request,
        ClaimsPrincipal user,
        IRateLimiter rateLimiter,
        IOptions<ChatRateLimitOptions> rateLimitOptions,
        ILogger<Endpoint> logger,
        SendMessage sendMessage,
        CancellationToken ct = default)
    {
        var userId = UserClaims.GetId(user);
        var options = rateLimitOptions.Value;

        var allowed = await rateLimiter.IsAllowedAsync($"chat:msg:{userId}", options.MessageLimit, options.MessageWindow);
        if (!allowed)
        {
            logger.LogWarning("Chat REST rate limit exceeded for user {UserId}", userId);
            return Results.Problem("You're sending messages too fast.", statusCode: StatusCodes.Status429TooManyRequests);
        }

        var (valid, error) = MessageValidator.Validate(request.Content);
        if (!valid)
        {
            logger.LogWarning("Invalid chat message from user {UserId}: {Error}", userId, error);
            return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);
        }

        var dto = await sendMessage.SendAsync(
            new SendMessageDto(userId, UserClaims.GetName(user), sessionId, request.Content), ct);

        return Results.Created($"/api/v1/chat/{sessionId}/messages", dto);
    }
}
