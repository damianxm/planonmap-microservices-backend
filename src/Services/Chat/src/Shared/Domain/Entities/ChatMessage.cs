namespace Chat.Shared.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public ChatSession Session { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
