//© FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.CombatMode;
using Content.Shared.FCB.Weapons.Components;
using Content.Shared.FCB.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.FCB.Weapons;

public sealed partial class GunAimingSystem : SharedGunAimingSystem
{
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AimStatusChangedEvent>(OnAimStatusChanged);
        SubscribeLocalEvent<GunAimableComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
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
}
