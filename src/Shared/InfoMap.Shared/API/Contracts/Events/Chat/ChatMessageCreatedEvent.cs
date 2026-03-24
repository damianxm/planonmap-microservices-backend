namespace InfoMap.Shared.API.Contracts.Events.Chat;

public record ChatMessageCreatedEvent(
    Guid MessageId,
    string SenderId,
    string SenderName,
    Guid SessionId,
    string Content,
    DateTime CreatedAt);
