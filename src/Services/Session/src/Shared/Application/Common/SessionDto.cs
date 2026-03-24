namespace Session.Shared.Application.Common;

public record SessionDto(Guid Id, string Code, string Name, DateTime CreatedAt);
