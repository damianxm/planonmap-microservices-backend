namespace Session.Shared.Domain.Entities;

public class SessionInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
