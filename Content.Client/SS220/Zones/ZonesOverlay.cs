// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Content.Shared.SS220.Zones.Components;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
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
        var query = _entManager.EntityQueryEnumerator<ZonesDataComponent>();

        while (query.MoveNext(out var uid, out var zonesData))
        {
            if (_transformSystem.GetMapId(uid) != args.MapId)
                continue;

            foreach (var netZone in zonesData.Zones)
            {
                var zone = _entManager.GetEntity(netZone);
                if (!_entManager.TryGetComponent<ZoneComponent>(zone, out var zoneComp))
                    continue;

                DrawZone(drawHandle, args.WorldBounds, xforms, uid, (zone, zoneComp));
            }
        }

        drawHandle.SetTransform(Matrix3x2.Identity);
        drawHandle.UseShader(null);
    }

    private void DrawZone(
        DrawingHandleWorld drawHandle,
        Box2Rotated worldBounds,
        EntityQuery<TransformComponent> xforms,
        EntityUid parent,
        Entity<ZoneComponent> zone)
    {
        foreach (var box in zone.Comp.Boxes)
        {
            var texture = _cache.GetTexture("/Textures/Interface/Nano/square.png");

            var xform = xforms.GetComponent(parent);
            var (_, _, worldMatrix, invWorldMatrix) = _transformSystem.GetWorldPositionRotationMatrixWithInv(xform, xforms);

            var bounds = invWorldMatrix.TransformBox(worldBounds).Enlarged(2);
            drawHandle.SetTransform(worldMatrix);

            var color = (zone.Comp.CurColor ?? zone.Comp.DefaultColor).WithAlpha(0.125f);
            drawHandle.DrawTextureRect(texture, box, color);
        }
    }
}
