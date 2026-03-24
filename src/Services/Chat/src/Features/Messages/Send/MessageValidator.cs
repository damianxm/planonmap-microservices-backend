namespace Chat.Features.Messages.Send;

internal static class MessageValidator
{
    internal const int MaxMessageLength = 500;

    internal static (bool ok, string? error) Validate(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return (false, "Message cannot be empty.");
        if (content.Length > MaxMessageLength)
            return (false, $"Message too long (max {MaxMessageLength} characters).");
        return (true, null);
    }
}
