// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.SS220.Overlays;

public sealed class ThermalVisionOverlay : IgnoreLightVisionOverlay
{
    private readonly TransformSystem _transformSystem = default!;

    public ThermalVisionOverlay(float showRadius) : base(showRadius)
    {
        _transformSystem = Entity.System<TransformSystem>();
    }
    protected override void Render(Entity<SpriteComponent, TransformComponent> ent,
                        MapId? map, DrawingHandleWorld handle, Angle eyeRot)
    {
        var (uid, sprite, xform) = ent;
        if (xform.MapID != map)
            return;

        var position = _transformSystem.GetWorldPosition(xform);
        var rotation = _transformSystem.GetWorldRotation(xform);

        sprite.Render(handle, eyeRot, rotation, position: position);
    }
}
