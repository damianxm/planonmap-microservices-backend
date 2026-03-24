using Chat.Features.Messages.Send;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Identity;
using InfoMap.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Chat.Features.Messages.Send.SendMessage;

namespace Chat.Features.Messages.Hub;

[Authorize]
public sealed class ChatHub : Hub<IChatHubClient>
{
    private readonly ChatDbContext _db;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<ChatHub> _logger;
    private readonly ChatRateLimitOptions _rateLimitOptions;
    private readonly SendMessage _sendMessage;

    public ChatHub(ChatDbContext db, IRateLimiter rateLimiter, ILogger<ChatHub> logger, IOptions<ChatRateLimitOptions> rateLimitOptions, SendMessage sendMessage)
    {
        _db = db;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _rateLimitOptions = rateLimitOptions.Value;
        _sendMessage = sendMessage;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Connected: {ConnectionId} ({Name})", Context.ConnectionId, UserClaims.GetName(Context.User!));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
            _logger.LogWarning(exception, "Disconnected with error: {ConnectionId}", Context.ConnectionId);
        else
            _logger.LogDebug("Disconnected: {ConnectionId}", Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSession(Guid sessionId)
    {
        var ct = Context.ConnectionAborted;

        var session = await PollingHelper.WaitForAsync(
            () => _db.ChatSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == sessionId, ct),
            ct);

        if (session is null)
        {
            _logger.LogWarning("Session {SessionId} not found after polling for {ConnectionId}", sessionId, Context.ConnectionId);
            throw new HubException("Session not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString(), ct);

        var history = await GetMessagesEndpoint.LoadPageAsync(_db, sessionId, 50, null, ct);
        await Clients.Caller.LoadHistory(history);

        var name = UserClaims.GetName(Context.User!);
        await Clients.OthersInGroup(sessionId.ToString()).ReceiveSystemMessage($"{name} joined.");
        _logger.LogInformation("{Name} ({ConnectionId}) joined session {SessionId}", name, Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());

        var name = UserClaims.GetName(Context.User!);
        await Clients.OthersInGroup(sessionId.ToString()).ReceiveSystemMessage($"{name} left.");
    }

    public async Task SendMessage(Guid sessionId, string content)
    {
        var userId = UserClaims.GetId(Context.User!);

        var allowed = await _rateLimiter.IsAllowedAsync($"chat:msg:{userId}", _rateLimitOptions.MessageLimit, _rateLimitOptions.MessageWindow);
        if (!allowed)
        {
            var violations = await _rateLimiter.TrackViolationAsync(userId);
            _logger.LogWarning("Rate limit violation {Count}/{Max} for user {UserId} ({ConnectionId})",
                violations, _rateLimitOptions.MaxViolationsBeforeDisconnect, userId, Context.ConnectionId);
            if (violations >= _rateLimitOptions.MaxViolationsBeforeDisconnect)
            {
                _logger.LogWarning("Aborting connection — spam threshold reached for {UserId} ({ConnectionId})", userId, Context.ConnectionId);
                Context.Abort();
                return;
            }
            throw new HubException("You're sending messages too fast. Please slow down.");
        }

        var (valid, error) = MessageValidator.Validate(content);
        if (!valid)
        {
            _logger.LogWarning("Invalid message content from {UserId} ({ConnectionId}): {Error}", userId, Context.ConnectionId, error);
            throw new HubException(error!);
        }

        var sessionExists = await _db.ChatSessions.AnyAsync(s => s.Id == sessionId, Context.ConnectionAborted);
        if (!sessionExists)
        {
            _logger.LogWarning("SendMessage to unknown session {SessionId} by {UserId} ({ConnectionId})", sessionId, userId, Context.ConnectionId);
            throw new HubException("Session not found.");
        }

        await _sendMessage.SendAsync(
            new SendMessageDto(userId, UserClaims.GetName(Context.User!), sessionId, content),
            Context.ConnectionAborted);
    }
}
