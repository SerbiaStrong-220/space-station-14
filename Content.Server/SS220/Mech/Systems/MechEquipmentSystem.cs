// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechEquipmentSystem : EntitySystem
{
    [Dependency] private readonly AltMechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AltMechEquipmentComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<AltMechEquipmentComponent, InsertEquipmentEvent>(OnInsertEquipment);
        SubscribeLocalEvent<AltMechEquipmentComponent, MechEquipmentInsertedEvent>(OnEquipmentInserted);
        SubscribeLocalEvent<AltMechEquipmentComponent, MechEquipmentRemovedEvent>(OnEquipmentRemoved);
    }

    private void OnUsed(Entity<AltMechEquipmentComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        if (args.User == mechComp.PilotSlot.ContainedEntity)
            return;

        //if (mechComp.EquipmentContainer.ContainedEntities.Count >= mechComp.MaxEquipmentAmount)
        //    return;

        //if (_whitelistSystem.IsWhitelistFail(mechComp.EquipmentWhitelist, args.Used))
        //    return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertPartEvent(), ent.Owner, target: mech, used: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnInsertEquipment(Entity<AltMechEquipmentComponent> ent, ref InsertEquipmentEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", ent.Owner)), args.Args.Target.Value);
        _mech.InsertEquipment(args.Args.Target.Value, ent.Owner);

        if (ent.Comp.EquipmentOwner != null)
            _mech.UpdateUserInterface((EntityUid)ent.Comp.EquipmentOwner);

        args.Handled = true;
    }

    private void OnEquipmentInserted(Entity<AltMechEquipmentComponent> ent, ref MechEquipmentInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        _actions.AddAction(args.Mech, ref ent.Comp.EquipmentAbilityAction, ent.Comp.EquipmentAbilityActionName, args.Mech);
    }

    private void OnEquipmentRemoved(Entity<AltMechEquipmentComponent> ent, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        _actions.RemoveAction(ent.Comp.EquipmentAbilityAction);
    }
}
