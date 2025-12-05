// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using static Content.Client.SS220.Overlays.BoxesOverlay;
using Content.Shared.SS220.Zones.Components;
using Content.Client.SS220.Zones.Systems;
using Robust.Client.Graphics;
using System.Numerics;
using Content.Shared.SS220.Maths;

namespace Content.Client.SS220.Zones.Overlays;

public sealed partial class ZonesBoxesOverlayProvider : BoxesOverlayProvider
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly ZonesSystem _zones;

    public ZonesBoxesOverlayProvider() : base()
    {
        _zones = _entityManager.System<ZonesSystem>();
    }

    public override List<BoxOverlayData> GetBoxesDatas()
    {
        List<BoxOverlayData> overlayData = [];
        var query = _entityManager.AllEntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            var alpha = uid == _zones.ControlWindow.SelectedZoneEntry?.ZoneEntity.Owner ? 0.25f : 0.125F;
            var color = zoneComp.Color.WithAlpha(alpha);
            foreach (var box in zoneComp.Area)
                overlayData.Add(new BoxOverlayData(uid, box, color));
        }

        return overlayData;
    }

    private List<AtlasedBoxData> GetAtlases(Box2 box, Texture texture, Vector2i? pixelGridSize = null)
    {
        pixelGridSize ??= new Vector2i(32, 32);
        var pixelBox = Box2.FromTwoPoints(box.BottomLeft * pixelGridSize.Value, box.TopRight * pixelGridSize.Value);
        var textureGridRects = MathHelperExtensions.GetIntersectsLatticeBoxes(pixelBox, texture.Size);
        var excess = MathHelperExtensions.SubstructBox(textureGridRects, pixelBox);
        textureGridRects = MathHelperExtensions.SubstructBoxes(textureGridRects, excess);

        var result = new List<AtlasedBoxData>();
        foreach (var rect in textureGridRects)
        {
            var left = rect.Left % texture.Size.X;
            var top = rect.Top % texture.Size.Y;

            if (left < 0)
                left += texture.Size.X;

            if (top < 0)
                top = Math.Abs(top);
            else if (top > 0)
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
