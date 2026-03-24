using Chat.Shared.Application.Common;

namespace Chat.Features.Messages;

public record MessagesPageDto(
    IReadOnlyList<ChatMessageDto> Items,
    DateTime? NextCursor);
