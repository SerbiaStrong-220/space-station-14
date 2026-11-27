// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Pinpointer.UI;
using Content.Shared.SS220.CultYogg.CultMiniMap;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapWindow
{
    private void UpdatePings(IEnumerable<CultMiniMapPing> pings)
    {
        NavMap.Pings.Clear();
        foreach (var ping in pings)
        {
            var coordinates = _entities.GetCoordinates(ping.Coordinates);
            if (!coordinates.IsValid(_entities))
                continue;

            NavMap.Pings[ping.Id] = new CultMiniMapPingBlip(
                ping.Id,
                coordinates,
                _sprites.Frame0(ping.Icon),
                ping.Color,
                ping.Scale);
        }
    }

    private void RefreshMapMarkers()
    {
        NavMap.TrackedEntities.Clear();
        NavMap.StructureMarkers.Clear();
        NavMap.LocalizedNames.Clear();

        foreach (var entity in _trackedEntities)
            AddMapMarker(entity);

        NavMap.UpdateStructureNeighbors();
    }

    private void AddMapMarker(CultMiniMapTrackedEntity trackedEntity)
    {
        var coordinates = _entities.GetCoordinates(trackedEntity.Coordinates);
        if (coordinates == null || !NavMap.Visible)
            return;

        if (trackedEntity.Marker.MarkerType != CultMiniMapMarkerType.Icon)
        {
            NavMap.StructureMarkers[trackedEntity.Entity] = new CultMiniMapStructureBlip(
                coordinates.Value,
                trackedEntity.Marker.MarkerType,
                trackedEntity.Marker.Color,
                trackedEntity.Rotation,
                trackedEntity.StructureLocation,
                CultMiniMapStructureNeighbors.None);
            return;
        }

        NavMap.TrackedEntities[trackedEntity.Entity] = new NavMapBlip(
            coordinates.Value,
            _sprites.Frame0(trackedEntity.Marker.Icon),
            trackedEntity.Marker.Color,
            trackedEntity.Entity == _selected,
            scale: trackedEntity.Marker.Scale);
        var role = GetMarkerLabel(trackedEntity.Marker);
        var details = trackedEntity.Name + ", " + role;
        if (trackedEntity.Marker.ShowHealth)
            details += "\n" + GetHealthStatus(trackedEntity);
        NavMap.LocalizedNames[trackedEntity.Entity] = details;
    }
}
