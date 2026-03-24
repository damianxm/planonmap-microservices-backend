namespace InfoMap.Shared.API.Contracts.Events.Session;

public record SessionCreatedEvent(
    Guid SessionId,
    string Name,
    string CreatedById,
    DateTime CreatedAt);
