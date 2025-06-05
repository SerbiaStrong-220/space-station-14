// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using static Content.Client.SS220.Overlays.BoxesOverlay;
using Content.Shared.SS220.Zones.Components;
using Content.Client.SS220.Zones.Systems;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;

namespace Content.Client.SS220.Zones.Overlays;

public sealed partial class ZonesBoxesOverlayProvider : BoxesOverlayProvider
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;

    private readonly ZonesSystem _zones;

    public ZonesBoxesOverlayProvider() : base()
    {
        _zones = _entityManager.System<ZonesSystem>();
    }

    public override List<BoxesOverlayData> GetBoxesDatas()
    {
        List<BoxesOverlayData> overlayData = [];
        var query = _entityManager.EntityQueryEnumerator<ZonesContainerComponent>();
        while (query.MoveNext(out var parent, out var container))
        {
            foreach (var netZone in container.Zones)
            {
                var zone = _entityManager.GetEntity(netZone);
                if (!_entityManager.TryGetComponent<ZoneComponent>(zone, out var zoneComp))
                    continue;

                var @params = zoneComp.ZoneParams;
                var alpha = zone == _zones.ControlWindow.SelectedZoneEntry?.ZoneEntity.Owner ? 0.25f : 0.125F;
                var color = @params.Color.WithAlpha(alpha);
                var currentData = new BoxesOverlayData(parent)
                {
                    Boxes = @params.CurrentSize,
                    Color = color,
                };
                overlayData.Add(currentData);

                var cutedTexture = _cache.GetTexture("/Textures/SS220/Interface/Zones/stripes.svg.192dpi.png");
                var cutedData = new BoxesOverlayData(parent)
                {
                    Boxes = @params.CutOutSize,
                    Color = color,
                    Texture = cutedTexture
                };
                overlayData.Add(cutedData);
            }
        }

        return overlayData;
    }
}
