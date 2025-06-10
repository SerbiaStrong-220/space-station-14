// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using static Content.Client.SS220.Overlays.BoxesOverlay;
using Content.Shared.SS220.Zones.Components;
using Content.Client.SS220.Zones.Systems;
using Robust.Client.ResourceManagement;
using Content.Client.Resources;
using Robust.Client.Graphics;
using System.Numerics;
using Content.Shared.SS220.Maths;

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

    public override List<BoxOverlayData> GetBoxesDatas()
    {
        List<BoxOverlayData> overlayData = [];
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
                foreach (var box in @params.ActiveRegion)
                    overlayData.Add(new BoxOverlayData(parent, box, color));

                var cutedTexture = _cache.GetTexture("/Textures/SS220/Interface/Zones/stripes.svg.192dpi.png");
                foreach (var box in @params.DisabledRegion)
                    foreach (var atlasedData in GetAtlases(box, cutedTexture))
                        overlayData.Add(new BoxOverlayData(parent, atlasedData.Box, color, atlasedData.Atlas));
            }
        }

        return overlayData;
    }

    private List<AtlasedBoxData> GetAtlases(Box2 box, Texture texture, Vector2i? pixelGridSize = null)
    {
        pixelGridSize ??= new Vector2i(32, 32);
        var pixelBox = Box2.FromTwoPoints(box.BottomLeft * pixelGridSize.Value, box.TopRight * pixelGridSize.Value);
        var textureGridRects = MathHelperExtensions.GetIntersectsGridBoxes(pixelBox, texture.Size);
        var excess = MathHelperExtensions.SubstructBox(textureGridRects, pixelBox);
        textureGridRects = MathHelperExtensions.SubstructBox(textureGridRects, excess);

        var result = new List<AtlasedBoxData>();
        foreach (var rect in textureGridRects)
        {
            var left = rect.Left % texture.Size.X;
            var top = rect.Top % texture.Size.Y;

            if (left < 0)
                left += texture.Size.X;

            if (top < 0)
                top += texture.Size.Y;

            if (top != 0)
                top = texture.Size.Y - top;

            var atlas = new AtlasTexture(texture, UIBox2.FromDimensions(new Vector2(left, top), rect.Size));
            var resultBox = Box2.FromTwoPoints(rect.BottomLeft / pixelGridSize.Value, rect.TopRight / pixelGridSize.Value);
            result.Add(new AtlasedBoxData(resultBox, atlas));
        }

        return result;
    }

    private struct AtlasedBoxData(Box2 box, AtlasTexture atlas)
    {
        public Box2 Box = box;
        public AtlasTexture Atlas = atlas;
    }
}
