// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Emp;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Upgrades;
using Content.Shared.Weapons.Ranged.Upgrades.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Weapons.Ranged.Upgrades;

public sealed partial class GunUpgradeModulesSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private GunUpgradeSystem _gunUpgrade = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, EmpAttemptEvent>(OnBatteryEmpAttempt);
        SubscribeLocalEvent<UpgradeableGunComponent, RefreshChargeRateEvent>(OnRefreshChargeRate);
        SubscribeLocalEvent<UpgradeableGunComponent, EntInsertedIntoContainerMessage>(OnUpgradesChanged);
        SubscribeLocalEvent<UpgradeableGunComponent, EntRemovedFromContainerMessage>(OnUpgradesChanged);
        SubscribeLocalEvent<GunUpgradeRechargeComponent, GunShotEvent>(OnRechargeGunShot);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GunUpgradeRechargeComponent>();
        while (query.MoveNext(out var uid, out var recharge))
        {
            if (recharge.PausedUntil is not { } until || _timing.CurTime < until)
                continue;

            recharge.PausedUntil = null;
            Dirty(uid, recharge);
            RefreshGunChargeRate(uid);
        }
    }

    private void OnBatteryEmpAttempt(Entity<BatteryComponent> ent, ref EmpAttemptEvent args)
    {
        if (args.Cancelled || !TryGetFuse(ent, out var gun, out var fuse))
            return;

        args.Cancelled = true;

        if (_timing.CurTime >= fuse.Value.Comp.ReadyTime)
        {
            fuse.Value.Comp.ReadyTime = _timing.CurTime + fuse.Value.Comp.Cooldown;
            Dirty(fuse.Value);
            _popup.PopupPredicted(Loc.GetString("gun-upgrade-emp-fuse-triggered"), gun, null);
            return;
        }

        _battery.SetCharge(ent.AsNullable(), 0);
        _popup.PopupPredicted(Loc.GetString("gun-upgrade-emp-fuse-burnt"), gun, null, PopupType.MediumCaution);
        PredictedQueueDel(fuse.Value.Owner);
    }

    private void OnRefreshChargeRate(Entity<UpgradeableGunComponent> ent, ref RefreshChargeRateEvent args)
    {
        foreach (var upgrade in _gunUpgrade.GetCurrentUpgrades(ent))
        {
            if (TryComp<GunUpgradeRechargeComponent>(upgrade, out var recharge) && recharge.PausedUntil == null)
                args.NewChargeRate += recharge.ChargeRate;
        }
    }

    private void OnUpgradesChanged<T>(Entity<UpgradeableGunComponent> ent, ref T args) where T : ContainerModifiedMessage
    {
        if (args.Container.ID == ent.Comp.UpgradesContainerId && HasComp<GunUpgradeRechargeComponent>(args.Entity))
            RefreshChargeRate(ent.Owner);
    }

    private void OnRechargeGunShot(Entity<GunUpgradeRechargeComponent> ent, ref GunShotEvent args)
    {
        if (ent.Comp.PauseOnShot <= TimeSpan.Zero)
            return;

        var wasPaused = ent.Comp.PausedUntil != null;
        ent.Comp.PausedUntil = _timing.CurTime + ent.Comp.PauseOnShot;
        Dirty(ent);

        if (!wasPaused)
            RefreshGunChargeRate(ent);
    }

    private void RefreshGunChargeRate(EntityUid upgrade)
    {
        if (_container.TryGetContainingContainer(upgrade, out var container))
            RefreshChargeRate(container.Owner);
    }

    private void RefreshChargeRate(EntityUid gun)
    {
        if (_powerCell.TryGetBatteryFromSlotOrEntity(gun, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }

    private bool TryGetFuse(EntityUid battery, out EntityUid gun, [NotNullWhen(true)] out Entity<GunUpgradeEmpFuseComponent>? fuse)
    {
        gun = battery;
        fuse = null;

        if (!TryComp<UpgradeableGunComponent>(gun, out var upgradeable))
        {
            if (!_container.TryGetContainingContainer(battery, out var container)
                || !TryComp<PowerCellSlotComponent>(container.Owner, out var slot)
                || container.ID != slot.CellSlotId
                || !TryComp(container.Owner, out upgradeable))
                return false;

            gun = container.Owner;
        }

        foreach (var upgrade in _gunUpgrade.GetCurrentUpgrades((gun, upgradeable)))
        {
            if (!TryComp<GunUpgradeEmpFuseComponent>(upgrade, out var fuseComp))
                continue;

            fuse = (upgrade, fuseComp);
            return true;
        }

        return false;
    }
}
