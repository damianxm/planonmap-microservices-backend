namespace Chat.Features.Messages.Send;

public record SendMessageDto(string SenderId, string SenderName, Guid SessionId, string Content);
