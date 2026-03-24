namespace MapItems.Features.Markers.Create;

public sealed class MarkerRateLimitOptions
{
    public int MarkerLimit { get; init; } = 10;
    public TimeSpan MarkerWindow { get; init; } = TimeSpan.FromMinutes(1);
}
