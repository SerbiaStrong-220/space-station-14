// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SiliconComponents;

public abstract partial class SharedSiliconComponentsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    private static readonly LocId NotEnoughSpace = "silicon-component-not-enough-space";
    private static readonly LocId InstallationBegun = "silicon-component-begin-install";
    private static readonly LocId RemovalBegun = "silicon-component-begin-removal";

    private static readonly string PartContainerPrefix = "silicon_component";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconComponentsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconComponentsComponent, AfterInteractUsingEvent>(OnSiliconInteractedWith);

        SubscribeLocalEvent<SiliconComponentsComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<SiliconComponentsComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);

        SubscribeLocalEvent<SiliconComponentsComponent, InstallSiliconPartEvent>(OnPartInstall);
        SubscribeLocalEvent<SiliconComponentsComponent, RemoveSiliconPartEvent>(OnPartRemove);

        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectPartBuiMessage>(OnEjectPartBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconRemoveModuleBuiMessage>(OnRemoveModuleBuiMessage);
    }

    private void OnComponentStartup(Entity<SiliconComponentsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(ent.Owner, out var containerManager))
            return;

        foreach (PartType part in Enum.GetValues(typeof(PartType)))
        {
            if (ent.Comp.Parts.ContainsKey(part))
                continue;

            ent.Comp.Parts.Add(part, _container.EnsureContainer<ContainerSlot>(ent.Owner, PartContainerPrefix + "_" + Enum.GetName(part), containerManager));
        }
    }

    private void OnSiliconInteractedWith(Entity<SiliconComponentsComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var used = args.Used;

        if (!TryComp<SiliconPartComponent>(used, out var partComp))
            return;

        if (partComp.OccupiedSpace > ent.Comp.ModuleSpace)
        {
            _popup.PopupEntity(Loc.GetString(NotEnoughSpace), args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!ent.Comp.Parts.TryGetValue(partComp.Type, out var container))
            return;

        if (container.ContainedEntity != null)
        {
            _popup.PopupEntity(Loc.GetString("silicon-component-slot-occupied"), args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString(InstallationBegun, ("item", args.Used)), ent.Owner);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, partComp.TimeToInstall, new InstallSiliconPartEvent(partComp.Type), ent.Owner, target: ent.Owner, used: args.Used)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnEntityInserted(Entity<SiliconComponentsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateUI(ent.AsNullable());
    }

    private void OnEntityRemoved(Entity<SiliconComponentsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateUI(ent.AsNullable());
    }

    public virtual void UpdateUI(Entity<SiliconComponentsComponent?> ent) { }

    private void OnEjectPartBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconEjectPartBuiMessage args)
    {
        if (!ent.Comp.Parts.TryGetValue(args.DesiredPart, out var container))
            return;

        if (container.ContainedEntity is not { Valid: true } part)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp))
            return;

        _popup.PopupEntity(Loc.GetString(RemovalBegun, ("item", args.Actor)), ent.Owner);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Actor, partComp.TimeToInstall, new RemoveSiliconPartEvent(args.DesiredPart), used: part, eventTarget: ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnEjectBatteryBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconEjectBatteryBuiMessage args)
    {
        if (_powerCell.TryEjectBatteryFromSlot(ent.Owner, out var powerCell, args.Actor))
            _hands.TryPickupAnyHand(args.Actor, powerCell.Value);
    }

    private void OnRemoveModuleBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconRemoveModuleBuiMessage args)
    {
        var module = GetEntity(args.Module);

        if (!ent.Comp.ModuleContainer.Contains(module))
            return;

        //if (!CanRemoveModule((module, Comp<BorgModuleComponent>(module))))
        //    return;

        //_adminLog.Add(LogType.Action, LogImpact.Medium,
        //    $"{args.Actor} removed module {module} from borg {chassis.Owner}");
        //_container.Remove(module, chassis.Comp.ModuleContainer);
        //_hands.TryPickupAnyHand(args.Actor, module);
    }

    private void OnPartInstall(Entity<SiliconComponentsComponent> ent, ref InstallSiliconPartEvent args)
    {
        if (args.Used is not { Valid: true } partValidated)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.Slot, out var container))
            return;

        if (container.ContainedEntity != null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(args.Used, out var partComp))
            return;

        _container.Insert(partValidated, container);

        var insertedEv = new ComponentGotInsertedIntoUser(ent.Owner);
        RaiseLocalEvent(partValidated, ref insertedEv);
    }

    private void OnPartRemove(Entity<SiliconComponentsComponent> ent, ref RemoveSiliconPartEvent args)
    {
        if (!ent.Comp.Parts.TryGetValue(args.Slot, out var container))
            return;

        if (container.ContainedEntity is not { Valid: true } part)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(part, out var partComp))
            return;

        _container.Remove(part, container);
        _hands.TryPickupAnyHand(args.User, part);

        var removedEv = new ComponentGotRemovedFromUser(ent.Owner);
        RaiseLocalEvent(part, ref removedEv);

        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class InstallSiliconPartEvent : SimpleDoAfterEvent
{
    public PartType Slot;

    public InstallSiliconPartEvent(PartType slot)
    {
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed partial class RemoveSiliconPartEvent : SimpleDoAfterEvent
{
    public PartType Slot;

    public RemoveSiliconPartEvent(PartType slot)
    {
        Slot = slot;
    }
}

[ByRefEvent]
public record struct ComponentGotInsertedIntoUser(EntityUid Owner)
{
}

[ByRefEvent]
public record struct ComponentGotRemovedFromUser(EntityUid Owner)
{
}

