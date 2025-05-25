// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using static Content.Client.SS220.Overlays.BoxesOverlay;
using Content.Shared.SS220.Zones.Components;
using Content.Client.SS220.Zones.Systems;
using Content.Shared.SS220.Zones.Systems;

namespace Content.Client.SS220.Zones.Overlays;

public sealed partial class ZonesBoxesDatasProvider : BoxesDatasProvider
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly ZonesSystem _zones;

    public ZonesBoxesDatasProvider() : base()
    {
        _zones = _entityManager.System<ZonesSystem>();
    }

    public override List<BoxesData> GetBoxesDatas()
    {
        List<BoxesData> boxes = [];
        var query = _entityManager.EntityQueryEnumerator<ZonesContainerComponent>();
        while (query.MoveNext(out var parent, out var container))
        {
            foreach (var netZone in container.Zones)
            {
                var zone = _entityManager.GetEntity(netZone);
                if (!_entityManager.TryGetComponent<ZoneComponent>(zone, out var zoneComp))
                    continue;

                var alpha = zone == _zones.SelectedZone?.Owner ? 0.25f : 0.125F;
                var color = (zoneComp.Color ?? SharedZonesSystem.DefaultColor).WithAlpha(alpha);
                var data = new BoxesData(parent)
                {
                    Boxes = zoneComp.Boxes,
                    Color = color,
                };

                boxes.Add(data);
            }
        }

        return boxes;
    }
}
