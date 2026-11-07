// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.SS220.Overlays;

public sealed class ThermalVisionOverlay : IgnoreLightVisionOverlay
{
    private readonly TransformSystem _transformSystem = default!;
    private readonly SpriteSystem _sprite = default!;

    public ThermalVisionOverlay(float showRadius, float closeShowRadius) : base(showRadius, closeShowRadius)
    {
        _transformSystem = Entity.System<TransformSystem>();
        _sprite = Entity.System<SpriteSystem>();
    }

    protected override void Render(Entity<SpriteComponent, TransformComponent> ent,
                        MapId? map, DrawingHandleWorld handle, Angle eyeRot)
    {
        var (_, _, xform) = ent;
        if (xform.MapID != map)
            return;

        var position = _transformSystem.GetWorldPosition(xform);
        var rotation = _transformSystem.GetWorldRotation(xform);

        _sprite.RenderSprite(ent, handle, eyeRot, rotation, position);
    }
}
