using Content.Server.Nuke;
using Content.Server.Popups;
using Content.Server.SS220.LockPick.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Lock;
using Content.Shared.SS220.LockPick;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.SS220.LockPick.Systems;

public sealed class LockpickSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LockpickComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<LockpickComponent, LockPickEvent>(OnLockPick);
    }

    private void OnAfterInteract(Entity<LockpickComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !HasComp<TargetLockPickComponent>(args.Target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            5f,
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

        if (!_random.Prob(targetLockPickComponent.ChanceToLockPick))
        {
            _popupSystem.PopupEntity(Loc.GetString("lockpick-failed"), args.User, args.User);
            return;
        }

        if (HasComp<NukeComponent>(args.Target))
        {
            var xform = Transform(args.Target.Value);

            if (xform.Anchored)
                _transform.Unanchor(args.Target.Value, xform);
        }

        if (HasComp<EntityStorageComponent>(args.Target))
        {
            _lockSystem.Unlock(args.Target.Value, args.User);
            _entityStorage.OpenStorage(args.Target.Value);
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/SS220/Effects/Drop/needle.ogg"), args.Target.Value);
        _popupSystem.PopupEntity(Loc.GetString("lockpick-successful"), args.User, args.User);
    }
}

