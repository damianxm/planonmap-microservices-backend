namespace Session.Shared.Application.Common;

public record ParticipantDto(string UserId, string DisplayName, DateTime JoinedAt, bool IsHost);
