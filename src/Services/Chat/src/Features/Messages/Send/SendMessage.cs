using Chat.Features.Messages.Hub;
using Chat.Shared.Application.Common;
using Chat.Shared.Domain.Entities;
using Chat.Shared.Infrastructure;
using InfoMap.Shared.API.Contracts.Events.Chat;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Features.Messages.Send;

public sealed class SendMessage(ChatDbContext db, IHubContext<ChatHub, IChatHubClient> hub, IPublishEndpoint publishEndpoint)
{
    public async Task<ChatMessageDto> SendAsync(SendMessageDto sendDto, CancellationToken ct = default)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = sendDto.SenderId,
            SenderName = sendDto.SenderName,
            SessionId = sendDto.SessionId,
            Content = sendDto.Content,
            CreatedAt = DateTime.UtcNow
        };

        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);

        var dto = new ChatMessageDto(
            message.Id, message.SenderId, message.SenderName,
            message.SessionId, message.Content, message.CreatedAt);

        await hub.Clients.Group(sendDto.SessionId.ToString()).ReceiveMessage(dto);

        await publishEndpoint.Publish(
            new ChatMessageCreatedEvent(dto.Id, dto.SenderId, dto.SenderName, dto.SessionId, dto.Content, dto.CreatedAt),
            ct);

        return dto;
    }
}
