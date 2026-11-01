using Content.Shared.SS220.DarkForces.Saint.Chaplain.Components;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;
using ChaplainBarrierComponent = Content.Shared.SS220.DarkForces.Saint.Chaplain.Components.ChaplainBarrierComponent;

namespace Content.Shared.SS220.DarkForces.Saint.Chaplain;

public sealed class SharedChaplainBarrierSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChaplainBarrierComponent, StartCollideEvent>(OnStartCollideBarrier);
        SubscribeLocalEvent<ChaplainBarrierComponent, PreventCollideEvent>(OnPreventCollideBarrier);
    }

    private void OnPreventCollideBarrier(EntityUid uid, ChaplainBarrierComponent component,
        ref PreventCollideEvent args)
    {
        if (!HasComp<ChaplainBarrierTargetComponent>(args.OtherEntity))
            args.Cancelled = true;
    }

    private void OnStartCollideBarrier(EntityUid uid, ChaplainBarrierComponent component, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (!HasComp<ChaplainBarrierTargetComponent>(target))
            return;

        var fieldDir = _transformSystem.GetWorldPosition(uid);
        var playerDir = _transformSystem.GetWorldPosition(target);

        _throwing.TryThrow(target, playerDir - fieldDir, 50);
    }
}
