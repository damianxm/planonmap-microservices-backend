namespace Chat.Features.Messages.Send;

public sealed class ChatRateLimitOptions
{
    public int MessageLimit { get; init; } = 5;
    public TimeSpan MessageWindow { get; init; } = TimeSpan.FromSeconds(5);
    public int MaxViolationsBeforeDisconnect { get; init; } = 3;
}
