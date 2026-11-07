// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Engineering.Components;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract partial class SharedGasWeaponSystem : EntitySystem
{

    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasWeaponComponent, ShotAttemptedEvent>(OnShootAttempt);
        SubscribeLocalEvent<GasWeaponComponent, ExaminedEvent>(OnBatteryExamine);
        SubscribeLocalEvent<GasWeaponComponent, GetVerbsEvent<AlternativeVerb>>(OnGasWeaponAltVerb);
        SubscribeLocalEvent<GasWeaponComponent, GasWeaponFlushTankDoAfterEvent>(OnDisassembleDoAfter);
    }

    protected void OnBatteryExamine(Entity<GasWeaponComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("gas-gun-examine",
                                ("stateText", Loc.GetString(ent.Comp.CanShoot
                                    ? "gas-gun-examine-ready"
                                    : "gas-gun-examine-unready"))));
    }

    protected virtual void OnShootAttempt(Entity<GasWeaponComponent> ent, ref ShotAttemptedEvent args)
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

    private void OnGasWeaponAltVerb(Entity<GasWeaponComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<GasTankComponent>(ent.Owner, out var gasTank))
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("gas-gun-flush-tank"),
            Impact = LogImpact.Low,
            Act = () => DoAfterReleaseGas(ent, user),
        });
    }

    private void DoAfterReleaseGas(Entity<GasWeaponComponent> ent, EntityUid user)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            ent.Comp.FlushTime,
            new GasWeaponFlushTankDoAfterEvent(),
            eventTarget: ent,
            ent)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDisassembleDoAfter(Entity<GasWeaponComponent> ent, ref GasWeaponFlushTankDoAfterEvent args)
    {
        if (!TryComp<GasTankComponent>(ent.Owner, out var gasTank))
            return;

        ReleaseGas(ent, gasTank);
    }

    protected virtual void ReleaseGas(Entity<GasWeaponComponent> ent, GasTankComponent tank)
    { }
}

[Serializable, NetSerializable]
public sealed partial class GasWeaponFlushTankDoAfterEvent : SimpleDoAfterEvent;
