// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using static Content.Client.SS220.Overlays.BoxesOverlay;
using Content.Shared.SS220.Zones.Components;
using Content.Client.SS220.Zones.Systems;

namespace Content.Client.SS220.Zones.Overlays;

public sealed partial class ZonesBoxesOverlayProvider : BoxesOverlayProvider
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly ZonesSystem _zones;

    private const float ColorAlpha = 0.125f;
    private const float SelectedColorAlpha = 0.25f;

    public ZonesBoxesOverlayProvider() : base()
    {
        _zones = _entityManager.System<ZonesSystem>();
    }

    public override List<BoxOverlayData> GetBoxesDatas()
    {
        List<BoxOverlayData> overlayData = [];

        var selectedZone = _zones.ControlWindow.EditingZoneEntry?.ZoneEntity.Owner;
        var query = _entityManager.AllEntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            if (_entityManager.Deleted(uid))
                continue;

            var alpha = uid == selectedZone ? SelectedColorAlpha : ColorAlpha;
            var color = zoneComp.Color.WithAlpha(alpha);
            foreach (var box in zoneComp.Area)
                overlayData.Add(new BoxOverlayData(uid, box, color));
        }

        return overlayData;
    }
}
