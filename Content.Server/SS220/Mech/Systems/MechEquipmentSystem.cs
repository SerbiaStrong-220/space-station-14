// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Parts.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs and it's logic
/// </summary>
public sealed class MechEquipmentSystem : EntitySystem
{
    [Dependency] private readonly AltMechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MechEquipmentActionComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<AltMechEquipmentComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<AltMechEquipmentComponent, InsertEquipmentEvent>(OnInsertEquipment);

        SubscribeLocalEvent<MechEquipmentActionComponent, MechEquipmentInsertedEvent>(OnEquipmentActionInserted);
        SubscribeLocalEvent<MechEquipmentActionComponent, MechEquipmentRemovedEvent>(OnEquipmentActionRemoved);

        SubscribeLocalEvent<MechEquipmentStatModifierComponent, MechEquipmentInsertedEvent>(OnStatsEquipmentInserted);
        SubscribeLocalEvent<MechEquipmentStatModifierComponent, MechEquipmentRemovedEvent>(OnStatsEquipmentRemoved);

        SubscribeLocalEvent<ToggleableMechEquipmentComponent, ToggleActionEvent>(OnEquipmentToggle);

        SubscribeLocalEvent<AltMechEquipmentComponent, MechEquipmentRelayedEvent<FlashAttemptEvent>>(OnFlashAttempt);
    }

    private void OnStartup(Entity<MechEquipmentActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.EquipmentAbilityAction, ent.Comp.EquipmentAbilityActionName, ent.Owner);
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

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertEquipmentEvent(), ent.Owner, target: mech, used: ent.Owner)
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

    private void OnEquipmentActionInserted(Entity<MechEquipmentActionComponent> ent, ref MechEquipmentInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        _actions.AddAction(args.Mech, ref ent.Comp.EquipmentAbilityAction, ent.Comp.EquipmentAbilityActionName, ent.Owner);
    }

    private void OnEquipmentActionRemoved(Entity<MechEquipmentActionComponent> ent, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        if (ent.Comp.EquipmentAbilityAction != null)
            _actions.RemoveProvidedAction(args.Mech, ent.Owner, (EntityUid)ent.Comp.EquipmentAbilityAction);
    }

    private void OnStatsEquipmentInserted(Entity<MechEquipmentStatModifierComponent> ent, ref MechEquipmentInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.MaxIntegrity += ent.Comp.MaxIntegrityDelta;

        mechComp.OwnMass += ent.Comp.OwnMassDelta;

        mechComp.MaximalArmMass += ent.Comp.MaximalArmMassDelta;
    }

    private void OnFlashAttempt(Entity<AltMechEquipmentComponent> ent, ref MechEquipmentRelayedEvent<FlashAttemptEvent> args)
    {
        if (TryComp<FlashImmunityComponent>(ent.Owner, out var _))
            args.Args.Cancelled = true;
    }

    private void OnStatsEquipmentRemoved(Entity<MechEquipmentStatModifierComponent> ent, ref MechEquipmentRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.MaxIntegrity -= ent.Comp.MaxIntegrityDelta;

        mechComp.OwnMass -= ent.Comp.OwnMassDelta;

        if(ent.Comp.MaximalArmMassDelta != 0)
        {
            var rightArm = mechComp.ContainerDict["right-arm"].ContainedEntity;

            if (rightArm != null && TryComp<MechPartComponent>(rightArm, out var partCompRight))
                if (partCompRight.OwnMass > mechComp.MaximalArmMass - ent.Comp.MaximalArmMassDelta)
                    _mech.RemovePart(args.Mech, (EntityUid)rightArm);

            var leftArm = mechComp.ContainerDict["left-arm"].ContainedEntity;

            if (leftArm != null && TryComp<MechPartComponent>(leftArm, out var partCompLeft))
                if (partCompLeft.OwnMass > mechComp.MaximalArmMass - ent.Comp.MaximalArmMassDelta)
                    _mech.RemovePart(args.Mech, (EntityUid)leftArm);
        }

        mechComp.MaximalArmMass -= ent.Comp.MaximalArmMassDelta;
    }

    private void OnEquipmentToggle(Entity<ToggleableMechEquipmentComponent> ent, ref ToggleActionEvent args)
    {
        args.Handled = _toggle.Toggle(ent.Owner, args.Performer);
    }
}
