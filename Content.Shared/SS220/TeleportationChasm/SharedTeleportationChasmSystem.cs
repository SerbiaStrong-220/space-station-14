// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.TeleportationChasm;

/// <summary>
///     Handles making entities fall into chasms when stepped on.
/// </summary>
public abstract class SharedTeleportationChasmSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGrapplingGunSystem _grapple = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportationChasmComponent, StepTriggeredOffEvent>(OnStepTriggered);
        SubscribeLocalEvent<TeleportationChasmComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
        SubscribeLocalEvent<TeleportationChasmFallingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
    }

    private void OnStepTriggered(Entity<TeleportationChasmComponent> ent, ref StepTriggeredOffEvent args)
    {
        if (HasComp<TeleportationChasmFallingComponent>(args.Tripper))
            return;

        StartFalling(ent, args.Tripper);
    }

    public void StartFalling(Entity<TeleportationChasmComponent> ent, EntityUid target, bool playSound = true)
    {
        var falling = AddComp<TeleportationChasmFallingComponent>(target);

        falling.NextDeletionTime = _timing.CurTime + falling.DeletionTime;
        _blocker.UpdateCanMove(target);

        if (playSound)
            _audio.PlayPredicted(ent.Comp.FallingSound, ent, target);
    }

    private void OnStepTriggerAttempt(Entity<TeleportationChasmComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (_grapple.IsEntityHooked(args.Tripper))
        {
            args.Cancelled = true;
            return;
        }

        args.Continue = true;
    }

    private void OnUpdateCanMove(Entity<TeleportationChasmFallingComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }
}
