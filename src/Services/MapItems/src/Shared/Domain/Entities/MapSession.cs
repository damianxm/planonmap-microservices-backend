namespace MapItems.Shared.Domain.Entities;

public class MapSession
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
