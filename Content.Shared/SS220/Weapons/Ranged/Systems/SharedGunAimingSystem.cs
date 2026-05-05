// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.CombatMode;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract partial class SharedGunAimingSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<AimStatusChangeAttemptEvent>(OnAimStatusChanged);
        SubscribeLocalEvent<GunAimableComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<CombatModeComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<GunAimableComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<GunAimableComponent, DroppedEvent>(OnDrop);
        SubscribeLocalEvent<GunAimableComponent, HandDeselectedEvent>(OnDeselect);
    }

    private void OnAimStatusChanged(AimStatusChangeAttemptEvent args)
    {
        EntityUid user = GetEntity(args.User);

        if (!TryComp<CombatModeComponent>(user, out var combatComp) || !combatComp.IsInCombatMode)
            return;

        if (!_gun.TryGetGun(user, out var gun) || !gun.Comp.UseKey)
            return;

        if (gun.Owner != GetEntity(args.Gun))
            return;

        if (!TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        aimableComp.IsAimed = args.Aim;

        if (_net.IsServer)
            Dirty(gun.Owner, aimableComp);

        _gun.RefreshModifiers((gun.Owner, gun));

        _movementSpeedModifier.RefreshMovementSpeedModifiers(user);
    }

    private void OnRefreshMovementSpeed(Entity<CombatModeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_gun.TryGetGun(ent.Owner, out var gun) || !TryComp<GunAimableComponent>(gun.Owner, out var aimableComp))
            return;

        if (aimableComp.AimedSprintSpeedModifier == null && aimableComp.AimedWalkingSpeedModifier == null)
            return;

        float SprintMod = 1f;
        float WalkMod = 1f;

        if (aimableComp.IsAimed)
        {
            if (aimableComp.AimedSprintSpeedModifier != null)
                SprintMod = (float)aimableComp.AimedSprintSpeedModifier;

            if (aimableComp.AimedWalkingSpeedModifier != null)
                WalkMod = (float)aimableComp.AimedWalkingSpeedModifier;

            args.ModifySpeed(WalkMod, SprintMod);
        }
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
        ent.Comp.IsAimed = false;
        Dirty(ent);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnDrop(Entity<GunAimableComponent> ent, ref DroppedEvent args)
    {
        ent.Comp.IsAimed = false;
        Dirty(ent);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnDeselect(Entity<GunAimableComponent> ent, ref HandDeselectedEvent args)
    {
        ent.Comp.IsAimed = false;
        Dirty(ent);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.User);
    }
}
