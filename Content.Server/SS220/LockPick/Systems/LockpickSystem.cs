using Content.Server.Popups;
using Content.Server.SS220.LockPick.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.LockPick;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.SS220.LockPick.Systems;

public sealed class LockpickSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockpickComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LockpickComponent, LockPickEvent>(OnLockPick);
    }

    private void OnAfterInteract(Entity<LockpickComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !TryComp<TargetLockPickComponent>(args.Target, out var targetLockPickComponent))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            targetLockPickComponent.TimeToLockPick * ent.Comp.LockPickSpeedModifier,
            new LockPickEvent(),
            ent.Owner,
            args.Target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnLockPick(Entity<LockpickComponent> ent, ref LockPickEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<TargetLockPickComponent>(args.Target, out var targetLockPickComponent))
            return;

        _audio.PlayPvs(ent.Comp.LockPickSound, args.Target.Value);

        if (!_random.Prob(targetLockPickComponent.ChanceToLockPick))
        {
            _popupSystem.PopupEntity(Loc.GetString("lockpick-failed"), args.User, args.User);
            return;
        }

        var ev = new LockPickSuccessEvent(args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        _popupSystem.PopupEntity(Loc.GetString("lockpick-successful"), args.User, args.User);
    }
}

