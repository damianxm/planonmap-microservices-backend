namespace Chat.Shared.Application.Common;

public record ChatMessageDto(
    Guid Id,
    string SenderId,
    string SenderName,
    Guid SessionId,
    string Content,
    DateTime CreatedAt);
