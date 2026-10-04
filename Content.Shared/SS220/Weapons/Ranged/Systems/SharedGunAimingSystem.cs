// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public sealed partial class SharedGunAimingSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunAimableComponent, AimStatusChangeAttemptEvent>(OnAimStatusChanged);
        SubscribeLocalEvent<GunAimableComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<CombatModeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<GunAimableComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<GunAimableComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<GunAimableComponent, HandDeselectedEvent>(OnDeselect);
        SubscribeLocalEvent<GunAimableComponent, HeldRelayedEvent<CombatModeDisabledEvent>>(OnCombatOff);

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.UseSecondary, InputCmdHandler.FromDelegate(OnAimEnabled, OnAimDisabled, handle: false, outsidePrediction: false))
            .Register<SharedGunAimingSystem>();
    }

    private void OnAimEnabled(ICommonSession? session)
    {
        OnAimToggleAttempt(session, true);
    }

    private void OnAimDisabled(ICommonSession? session)
    {
        OnAimToggleAttempt(session, false);
    }

    private void OnAimToggleAttempt(ICommonSession? session, bool enabled)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } user)
            return;

        if (!TryComp(user, out CombatModeComponent? combatComp) ||
            !combatComp.IsInCombatMode)
            return;

        if (!_gun.TryGetGun(user, out var gun) || !gun.Comp.UseKey)
            return;

        if (!TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        var useKey = EngineKeyFunctions.UseSecondary;

        if (enabled && !aimableComp.IsAimed)
        {
            var ev = new AimStatusChangeAttemptEvent { Aim = true, User = user };
            RaiseLocalEvent(gun.Owner, ref ev);
            return;
        }

        if (!enabled && aimableComp.IsAimed)
        {
            var ev = new AimStatusChangeAttemptEvent { Aim = false, User = user };
            RaiseLocalEvent(gun.Owner, ref ev);
        }
    }

    private void OnAimStatusChanged(Entity<GunAimableComponent> ent, ref AimStatusChangeAttemptEvent args)
    {
        if (args.User is not { Valid: true } user)
            return;

        if (!TryComp<CombatModeComponent>(user, out var combatComp) || !combatComp.IsInCombatMode)
            return;

        if (!_gun.TryGetGun(user, out var gun) || !gun.Comp.UseKey)
            return;

        if (gun.Owner != ent.Owner)
            return;

        if (!TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        aimableComp.IsAimed = args.Aim;

        Dirty(gun.Owner, aimableComp);

        _gun.RefreshModifiers((gun.Owner, gun));

        _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }

    private void OnRefreshMovementSpeed(Entity<CombatModeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_gun.TryGetGun(ent.Owner, out var gun) ||
            !TryComp<GunAimableComponent>(gun.Owner, out var aimableComp) ||
            !aimableComp.IsAimed)
            return;

        if (aimableComp.AimedSprintSpeedModifier == null &&
            aimableComp.AimedWalkingSpeedModifier == null)
            return;

        var sprintMod = aimableComp.AimedSprintSpeedModifier ?? 1f;
        var walkMod = aimableComp.AimedWalkingSpeedModifier ?? 1f;

        args.ModifySpeed(walkMod, sprintMod);
    }

    private void OnGunRefreshModifiers(Entity<GunAimableComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!ent.Comp.IsAimed)
            return;

        args.MinAngle += ent.Comp.MinAngle;
        args.MaxAngle += ent.Comp.MaxAngle;
        args.AngleDecay += ent.Comp.AngleDecay;
        args.AngleIncrease += ent.Comp.AngleIncrease;
    }

    private void OnUnequip(Entity<GunAimableComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopAiming(ent, args.User);
    }

    private void OnDrop(Entity<GunAimableComponent> ent, ref DroppedEvent args)
    {
        StopAiming(ent, args.User);
    }

    private void OnDeselect(Entity<GunAimableComponent> ent, ref HandDeselectedEvent args)
    {
        StopAiming(ent, args.User);
    }

    private void OnCombatOff(Entity<GunAimableComponent> ent, ref HeldRelayedEvent<CombatModeDisabledEvent> args)
    {
        if (args.Owner is { Valid: true } userValid)
            StopAiming(ent, userValid);
    }

    private void StopAiming(Entity<GunAimableComponent> ent, EntityUid user)
    {
        ent.Comp.IsAimed = false;
        _gun.RefreshModifiers(ent.Owner);

        Dirty(ent);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }
}
