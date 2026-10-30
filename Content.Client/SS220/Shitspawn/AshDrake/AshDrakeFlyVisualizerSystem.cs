// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeFlyVisualizerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeFlyComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, AshDrakeFlyComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.Color = comp.IsFlying
            ? new Robust.Shared.Maths.Color(0f, 0f, 0f, 0.6f)
            : Robust.Shared.Maths.Color.White;
    }
}
