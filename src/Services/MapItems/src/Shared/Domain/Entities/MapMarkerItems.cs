namespace MapItems.Shared.Domain.Entities;

public class MapMarkerItems
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public Guid SessionId { get; set; }
    public MapSession Session { get; set; } = null!;
}
