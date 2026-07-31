// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Actions.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Wires;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SiliconComponents;

public abstract partial class SharedSiliconComponentsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private static readonly LocId NotEnoughSpace = "silicon-component-not-enough-space";
    private static readonly LocId InstallationBegun = "silicon-component-begin-install";
    private static readonly LocId RemovalBegun = "silicon-component-begin-removal";

    private static readonly string PartContainerPrefix = "silicon_component";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponentsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SiliconComponentsComponent, AfterInteractUsingEvent>(OnSiliconInteractedWith);

        SubscribeLocalEvent<SiliconComponentsComponent, EntInsertedIntoContainerMessage>(OnEntityInserted);
        SubscribeLocalEvent<SiliconComponentsComponent, EntRemovedFromContainerMessage>(OnEntityRemoved);

        SubscribeLocalEvent<SiliconComponentsComponent, InstallSiliconPartEvent>(OnPartInstall);
        SubscribeLocalEvent<SiliconComponentsComponent, RemoveSiliconPartEvent>(OnPartRemove);

        SubscribeLocalEvent<SiliconComponentsComponent, InstallSiliconModuleEvent>(OnModInstall);
        SubscribeLocalEvent<SiliconComponentsComponent, RemoveSiliconModuleEvent>(OnModRemove);

        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectPartBuiMessage>(OnEjectPartBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconEjectBatteryBuiMessage>(OnEjectBatteryBuiMessage);
        SubscribeLocalEvent<SiliconComponentsComponent, SiliconRemoveModuleBuiMessage>(OnRemoveModuleBuiMessage);
    }

    private void OnComponentStartup(Entity<SiliconComponentsComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<ContainerManagerComponent>(ent.Owner, out var containerManager))
            return;

        if (TryComp<BlindableComponent>(ent.Owner, out var ownerBlindableComp))
            _blindable.UpdateIsBlind(ent.Owner);

        foreach (PartType part in Enum.GetValues(typeof(PartType)))
        {
            if (ent.Comp.Parts.ContainsKey(part))
                continue;

            ent.Comp.Parts.Add(part, _container.EnsureContainer<ContainerSlot>(ent.Owner, PartContainerPrefix + "_" + Enum.GetName(part), containerManager));
        }

        ent.Comp.ModuleContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ModuleContainerId, containerManager);
    }

    public override void Update(float frameTime)
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SiliconComponentsComponent>();
        while (query.MoveNext(out var uid, out var siliconComponent))
        {
            if (curTime < siliconComponent.NextBatteryUpdate)
                continue;

            siliconComponent.NextBatteryUpdate = curTime + TimeSpan.FromSeconds(1);
            Dirty(uid, siliconComponent);

            // If we aren't drawing and suddenly get enough power to draw again, reenable.
            //TryActivate((uid, siliconComponent));
            if (_powerCell.TryUseCharge(uid, siliconComponent.ChargeToUse.Float()))
            {
                if (siliconComponent.Online)
                    return;

                _statusEffects.TryRemoveStatusEffect(uid, "StatusEffectPowerOffline");
                siliconComponent.Online = true;
                Dirty(uid, siliconComponent);
                _blindable.UpdateIsBlind(uid);

                return;
            }

            if (!siliconComponent.Online)
                return;

            _statusEffects.TrySetStatusEffectDuration(uid, "StatusEffectPowerOffline", new TimeSpan(0, 0, 30));
            siliconComponent.Online = false;
            Dirty(uid, siliconComponent);
            _blindable.UpdateIsBlind(uid);
        }
    }

    private void OnSiliconInteractedWith(Entity<SiliconComponentsComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var used = args.Used;

        int occupiedSpace = 0;

        if (TryComp<SiliconPartComponent>(used, out var partComp))
            occupiedSpace = partComp.OccupiedSpace;

        if (TryComp<SiliconModuleComponent>(used, out var modComp))
            occupiedSpace = modComp.OccupiedSpace;

        if (occupiedSpace > ent.Comp.ModuleSpace)
        {
            _popup.PopupEntity(Loc.GetString(NotEnoughSpace), args.User);
            return;
        }

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (partComp != null)
        {
            if (!ent.Comp.Parts.TryGetValue(partComp.PartType, out var container))
                return;

            if (container.ContainedEntity != null)
            {
                _popup.PopupEntity(Loc.GetString("silicon-component-slot-occupied"), args.User);
                return;
            }

            _popup.PopupEntity(Loc.GetString(InstallationBegun, ("item", args.Used)), ent.Owner);

            var partDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, partComp.TimeToInstall, new InstallSiliconPartEvent(partComp.PartType), ent.Owner, target: ent.Owner, used: args.Used)
            {
                BreakOnMove = true,
            };

            _doAfter.TryStartDoAfter(partDoAfterEventArgs);
            args.Handled = true;
        }

        if (modComp != null)
        {
            if (ent.Comp.ModuleContainer == null)
                return;

            _popup.PopupEntity(Loc.GetString(InstallationBegun, ("item", args.Used)), ent.Owner);

            var partDoAfterEventArgs = new DoAfterArgs(EntityManager, args.User, modComp.TimeToInstall, new InstallSiliconModuleEvent(), ent.Owner, target: ent.Owner, used: args.Used)
            {
                BreakOnMove = true,
            };

            _doAfter.TryStartDoAfter(partDoAfterEventArgs);
            args.Handled = true;
        }
    }

    private void OnEntityInserted(Entity<SiliconComponentsComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        string containerID = args.Container.ID;

        if (TryComp<SiliconPartComponent>(args.Entity, out var partComp) && containerID.StartsWith(PartContainerPrefix))
        {
            partComp.PartOwner = ent.Owner;

            ent.Comp.ModuleSpace -= partComp.OccupiedSpace;

            var insertedEv = new ComponentGotInsertedIntoUser(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new ComponentInsertedIntoUser(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        if (TryComp<SiliconModuleComponent>(args.Entity, out var moduleComp) && containerID.StartsWith(PartContainerPrefix))
        {
            moduleComp.ModuleOwner = ent.Owner;

            ent.Comp.ModuleSpace -= moduleComp.OccupiedSpace;

            var insertedEv = new SiliconModuleGotInserted(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new SiliconModuleInserted(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        Dirty(ent);
    }

    private void OnEntityRemoved(Entity<SiliconComponentsComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        string containerID = args.Container.ID;

        if (TryComp<SiliconPartComponent>(args.Entity, out var partComp) && containerID.StartsWith(PartContainerPrefix))
        {
            partComp.PartOwner = null;

            ent.Comp.ModuleSpace += partComp.OccupiedSpace;

            var removedEv = new ComponentGotRemovedFromUser(ent.Owner);
            RaiseLocalEvent(args.Entity, ref removedEv);

            var userEv = new ComponentRemovedFromUser(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        if (TryComp<SiliconModuleComponent>(args.Entity, out var moduleComp) && containerID.StartsWith(PartContainerPrefix))
        {
            moduleComp.ModuleOwner = null;

            ent.Comp.ModuleSpace += moduleComp.OccupiedSpace;

            var insertedEv = new SiliconModuleGotRemoved(ent.Owner);
            RaiseLocalEvent(args.Entity, ref insertedEv);

            var userEv = new SiliconModuleRemoved(args.Entity);
            RaiseLocalEvent(ent.Owner, ref userEv);

            UpdateUI(ent.AsNullable());
        }

        Dirty(ent);
    }

    public virtual void UpdateUI(Entity<SiliconComponentsComponent?> ent) { }

    private void OnEjectPartBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconEjectPartBuiMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.DesiredPart, out var container))
            return;

        if (container.ContainedEntity is not { Valid: true } part)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(part, out var partComp))
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
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_powerCell.TryEjectBatteryFromSlot(ent.Owner, out var powerCell, args.Actor))
            _hands.TryPickupAnyHand(args.Actor, powerCell.Value);
    }

    private void OnRemoveModuleBuiMessage(Entity<SiliconComponentsComponent> ent, ref SiliconRemoveModuleBuiMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryGetEntity(args.Module, out var module) ||
            module is not { Valid: true } moduleValidated)
            return;

        if (!ent.Comp.ModuleContainer.Contains(moduleValidated))
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconModuleComponent>(moduleValidated, out var moduleComp))
            return;

        _popup.PopupEntity(Loc.GetString(RemovalBegun, ("item", args.Actor)), ent.Owner);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Actor, moduleComp.TimeToInstall, new RemoveSiliconModuleEvent(), used: moduleValidated, eventTarget: ent.Owner, target: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnPartInstall(Entity<SiliconComponentsComponent> ent, ref InstallSiliconPartEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Used is not { Valid: true } partValidated)
            return;

        if (!ent.Comp.Parts.TryGetValue(args.Slot, out var container))
            return;

        if (container.ContainedEntity != null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconPartComponent>(partValidated, out var partComp))
            return;

        _container.Insert(partValidated, container);

        Dirty(partValidated, partComp);
    }

    private void OnPartRemove(Entity<SiliconComponentsComponent> ent, ref RemoveSiliconPartEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

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

        Dirty(part, partComp);

        args.Handled = true;
    }

    private void OnModInstall(Entity<SiliconComponentsComponent> ent, ref InstallSiliconModuleEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Used is not { Valid: true } modValidated)
            return;

        if (ent.Comp.ModuleContainer == null)
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        if (!TryComp<SiliconModuleComponent>(modValidated, out var partComp))
            return;

        _container.Insert(modValidated, ent.Comp.ModuleContainer);

        Dirty(modValidated, partComp);
    }

    private void OnModRemove(Entity<SiliconComponentsComponent> ent, ref RemoveSiliconModuleEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Used is not { Valid: true } modValidated)
            return;

        if (ent.Comp.ModuleContainer == null)
            return;

        if (!ent.Comp.ModuleContainer.Contains(modValidated))
            return;

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panelComp) && !panelComp.Open)
            return;

        _container.Remove(modValidated, ent.Comp.ModuleContainer);
        _hands.TryPickupAnyHand(args.User, modValidated);

        if (TryComp<SiliconModuleComponent>(modValidated, out var modComp))
            Dirty(modValidated, modComp);

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

[Serializable, NetSerializable]
public sealed partial class InstallSiliconModuleEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveSiliconModuleEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public record struct ComponentGotInsertedIntoUser(EntityUid Owner)
{
}

[ByRefEvent]
public record struct ComponentGotRemovedFromUser(EntityUid Owner)
{
}

[ByRefEvent]
public record struct ComponentInsertedIntoUser(EntityUid Part)
{
}

[ByRefEvent]
public record struct ComponentRemovedFromUser(EntityUid Part)
{
}

