using MapItems.Shared.Application.Contracts;

namespace MapItems.Features.Markers.Hub;

public interface IMapItemsHubClient
{
    Task LoadMarkers(IReadOnlyList<MarkerDto> markers);
    Task ReceiveMarker(MarkerDto marker);
}