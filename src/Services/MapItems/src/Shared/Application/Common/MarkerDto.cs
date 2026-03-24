namespace MapItems.Shared.Application.Contracts;

public record MarkerDto(int Id, string Name, string? Description, float Latitude, float Longitude, Guid SessionId);
