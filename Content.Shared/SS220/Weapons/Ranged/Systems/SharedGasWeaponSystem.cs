// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract class SharedGasWeaponSystem : EntitySystem
{

    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasWeaponComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<GasWeaponComponent, ExaminedEvent>(OnBatteryExamine);
    }

    protected virtual void OnBatteryExamine(Entity<GasWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gas-gun-examine",
                                ("stateText", Loc.GetString(ent.Comp.CanShoot
                                    ? "gas-gun-examine-ready"
                                    : "gas-gun-examine-unready"))));
    }

    protected virtual void OnShootAttempt(Entity<GasWeaponComponent>ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.CanShoot)
        {
            args.Cancel();
            _popup.PopupCursor(Loc.GetString("gas-gun-fired-empty"));
            return;
        }

        if (ent.Comp.GasUsage == 0f)
            return;

        if ((!_container.TryGetContainer(ent.Owner, ent.Comp.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is null) && !ent.Comp.InternalTank)
            args.Cancel();
    }

}
