namespace Chat.Shared.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<ChatMessage> Messages { get; set; } = [];
}
