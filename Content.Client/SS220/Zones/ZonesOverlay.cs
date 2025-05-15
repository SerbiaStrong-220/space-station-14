
using Content.Client.Resources;
using Content.Shared.SS220.Zones.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client.SS220.Zones;

public sealed class ZonesOverlay : Overlay
{
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SharedTransformSystem _transformSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private ShaderInstance _shader;

    public ZonesOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index<ShaderPrototype>("unshaded").Instance();
        _transformSystem = _entManager.System<SharedTransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var drawHandle = args.WorldHandle;
        drawHandle.UseShader(_shader);

        var xforms = _entManager.GetEntityQuery<TransformComponent>();
        var query = _entManager.EntityQueryEnumerator<MapGridComponent, ZonesDataComponent>();

        while (query.MoveNext(out var uid, out var grid, out var zonesData))
        {
            if (_transformSystem.GetMapId(uid) != args.MapId)
                continue;

            foreach (var zone in zonesData.Zones.Values)
                DrawZone(drawHandle, args.WorldBounds, xforms, (uid, grid, zonesData), zone);
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawZone(
        DrawingHandleWorld drawHandle,
        Box2Rotated worldBounds,
        EntityQuery<TransformComponent> xforms,
        Entity<MapGridComponent, ZonesDataComponent> grid,
        ZoneData zoneData)
    {
        foreach (var tile in zoneData.Tiles)
        {
            var tileSize = grid.Comp1.TileSize;
            var centre = (tile + Vector2Helpers.Half) * tileSize;

            var texture = _cache.GetTexture("/Textures/Interface/Nano/square.png");

            var xform = xforms.GetComponent(grid);
            var (_, _, worldMatrix, invWorldMatrix) = _transformSystem.GetWorldPositionRotationMatrixWithInv(xform, xforms);

            var gridBounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(tileSize * 2);
            if (!gridBounds.Contains(centre))
                continue;

            var color = new Color(zoneData.Color.R, zoneData.Color.G, zoneData.Color.B, 128);
            drawHandle.DrawTextureRect(texture, Box2.CenteredAround(centre, new Vector2(tileSize, tileSize)), color);
        }
    }
}
