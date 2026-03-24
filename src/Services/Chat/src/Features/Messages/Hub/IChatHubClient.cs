using Chat.Shared.Application.Common;

namespace Chat.Features.Messages.Hub;

public interface IChatHubClient
{
    Task ReceiveMessage(ChatMessageDto message);
    Task ReceiveSystemMessage(string message);
    Task LoadHistory(MessagesPageDto history);
}
