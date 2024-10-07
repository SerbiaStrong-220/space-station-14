// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Overlays;

public sealed class KeenHearingOverlay : IgnoreLightVisionOverlay
{
    private readonly TransformSystem _transformSystem = default!;
    private readonly IGameTiming _gameTiming = default!;
    private readonly SpriteSystem _spriteSystem = default!;

    private SpriteSpecifier _sprite;
    private Texture _texture;

    private List<string> _blacklistComponentNames = new List<string> {"DarkReaper"};

    public KeenHearingOverlay(float showRadius, float closeShowRadius) : base(showRadius, closeShowRadius)
    {
        _transformSystem = Entity.System<TransformSystem>();
        _spriteSystem = Entity.System<SpriteSystem>();
        _gameTiming = IoCManager.Resolve<IGameTiming>();

        _sprite = new SpriteSpecifier.Rsi(new("/Textures/SS220/Effects/keen_vision_marker.rsi"), "pulse");
        _texture = _spriteSystem.GetFrame(_sprite, _gameTiming.CurTime);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        _texture = _spriteSystem.GetFrame(_sprite, _gameTiming.CurTime);

        base.Draw(args);
    }

    protected override void Render(Entity<SpriteComponent, TransformComponent> ent,
                        MapId? map, DrawingHandleWorld handle, Angle eyeRot)
    {
        var (uid, _, xform) = ent;

        if (uid == PlayerManager.LocalEntity)
            return;

        if (xform.MapID != map)
            return;

        if (HasComponentFromList(uid, _blacklistComponentNames))
            return;

        var position = _transformSystem.GetWorldPosition(xform);
        var rotation = Angle.Zero;
        var gridUid = _transformSystem.GetGrid((uid, xform));
        if (gridUid.HasValue)
            rotation = _transformSystem.GetWorldRotation(gridUid.Value);


        handle.SetTransform(position, rotation);
        // we need to offset it because it give us center center position, but we need bottom lefts
        handle.DrawTexture(_texture, new System.Numerics.Vector2(-0.5f));
    }
}
