namespace MapItems.Shared.Domain.Common;

internal static class MarkerContentRules
{
    internal const int MaxNameLength = 100;
    internal const int MaxDescriptionLength = 300;

    internal static (bool ok, string? error) ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Marker name cannot be empty.");
        if (name.Length > MaxNameLength)
            return (false, $"Marker name too long (max {MaxNameLength} characters).");
        return (true, null);
    }

    internal static (bool ok, string? error) ValidateDescription(string? description)
    {
        if (description is not null && description.Length > MaxDescriptionLength)
            return (false, $"Marker description too long (max {MaxDescriptionLength} characters).");
        return (true, null);
    }
}
