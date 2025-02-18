using Content.Server.SS220.LockPick.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.LockPick;

namespace Content.Server.SS220.LockPick.Systems;

public sealed class LockpickSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockpickComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<LockpickComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<TargetLockPickComponent>(args.Target, out var targetLockPickComponent))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            targetLockPickComponent.TimeToLockPick * ent.Comp.LockPickSpeedModifier,
            new LockPickEvent(),
            args.Target,
            ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }
}

