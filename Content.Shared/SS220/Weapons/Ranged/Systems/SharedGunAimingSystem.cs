// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract partial class SharedGunAimingSystem : EntitySystem
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<AimStatusChangeAttemptEvent>(OnAimStatusChanged);
        SubscribeLocalEvent<GunAimableComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<CombatModeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<GunAimableComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<GunAimableComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<GunAimableComponent, HandDeselectedEvent>(OnDeselect);
        SubscribeLocalEvent<GunAimableComponent, HeldRelayedEvent<CombatModeOffEvent>>(OnCombatOff);
    }

    private void OnAimStatusChanged(AimStatusChangeAttemptEvent message, EntitySessionEventArgs args)
    {
        EntityUid user = GetEntity(message.User);

        if (args.SenderSession.AttachedEntity != user)
            return;

        if (!TryComp<CombatModeComponent>(user, out var combatComp) || !combatComp.IsInCombatMode)
            return;

        if (!_gun.TryGetGun(user, out var gun) || !gun.Comp.UseKey)
            return;

        if (gun.Owner != GetEntity(message.Gun))
            return;

        if (!TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        aimableComp.IsAimed = message.Aim;

        if (_net.IsServer)
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

    private void OnCombatOff(Entity<GunAimableComponent> ent, ref HeldRelayedEvent<CombatModeOffEvent> args)
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
