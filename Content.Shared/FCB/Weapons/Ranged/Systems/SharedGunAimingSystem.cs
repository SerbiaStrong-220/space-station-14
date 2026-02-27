// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.CombatMode;
using Content.Shared.FCB.Weapons.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;

namespace Content.Shared.FCB.Weapons.Ranged.Systems;

public abstract partial class SharedGunAimingSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<AimStatusChangedEvent>(OnAimStatusChanged);
        SubscribeLocalEvent<GunAimableComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<GunAimableComponent, GotUnequippedHandEvent>(OnUnequip);
        SubscribeLocalEvent<GunAimableComponent, DroppedEvent>(OnDrop);
    }

    private void OnAimStatusChanged(AimStatusChangedEvent args)
    {
        EntityUid user = GetEntity(args.User);

        if (!TryComp<CombatModeComponent>(user, out var combatComp) || !combatComp.IsInCombatMode)
            return;

        if (!_gun.TryGetGun(user, out var gunUid, out var gun) || !gun.UseKey)
            return;

        if (gunUid != GetEntity(args.Gun))
            return;

        if (!TryComp<GunAimableComponent>(gunUid, out var aimableComp))
            return;

        aimableComp.IsAimed = args.Aim;

        if (_net.IsServer)
            Dirty(gunUid, aimableComp);

        _gun.RefreshModifiers((gunUid, gun));
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
    }

    private void OnDrop(Entity<GunAimableComponent> ent, ref DroppedEvent args)
    {
        ent.Comp.IsAimed = false;
    }
}
