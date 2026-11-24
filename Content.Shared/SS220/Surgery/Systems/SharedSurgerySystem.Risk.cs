// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Placeable;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float UnanesthetizedFailurePenalty = 0.25f;
    private const float NoOperatingTableFailurePenalty = 0.20f;
    private const float NoOperatingTableDelayMultiplier = 1.35f;
    private const float FloorFailurePenalty = 0.35f;
    private const float FloorDelayMultiplier = 1.5f;

    private void InitializeRisk()
    {
        SubscribeLocalEvent<SurgeryToolComponent, GetSurgeryDelayModifiersEvent>(OnToolGetDelayModifiers);
        SubscribeLocalEvent<SurgeryToolComponent, GetSurgeryFailureChanceEvent>(OnToolGetFailureChance);
        SubscribeLocalEvent<OperatingTableComponent, ComponentInit>(OnOperatingTableInit);
        SubscribeLocalEvent<OperatingTableComponent, PortDisconnectedEvent>(OnOperatingTablePortDisconnected);
        SubscribeLocalEvent<OperatingTableComponent, AfterInteractUsingEvent>(OnOperatingTableAfterInteractUsing, before: [typeof(PlaceableSurfaceSystem)]);
        SubscribeLocalEvent<OperatingTableComponent, GetSurgeryDelayModifiersEvent>(OnTableGetDelayModifiers);
        SubscribeLocalEvent<OperatingTableComponent, GetSurgeryFailureChanceEvent>(OnTableGetFailureChance);
        SubscribeLocalEvent<OperatingComputerComponent, ComponentInit>(OnOperatingComputerInit);
        SubscribeLocalEvent<OperatingComputerComponent, MapInitEvent>(OnOperatingComputerMapInit);
        SubscribeLocalEvent<OperatingComputerComponent, NewLinkEvent>(OnOperatingComputerNewLink);
        SubscribeLocalEvent<OperatingComputerComponent, PortDisconnectedEvent>(OnOperatingComputerPortDisconnected);
    }

    private void OnToolGetDelayModifiers(Entity<SurgeryToolComponent> entity, ref GetSurgeryDelayModifiersEvent args)
    {
        args.Multiplier *= entity.Comp.SpeedModifier;
    }

    private void OnToolGetFailureChance(Entity<SurgeryToolComponent> entity, ref GetSurgeryFailureChanceEvent args)
    {
        args.FlatModifier += entity.Comp.FailureModifier;
    }

    private void OnTableGetDelayModifiers(Entity<OperatingTableComponent> entity, ref GetSurgeryDelayModifiersEvent args)
    {
        args.Multiplier *= entity.Comp.SpeedModifier;
    }

    private void OnTableGetFailureChance(Entity<OperatingTableComponent> entity, ref GetSurgeryFailureChanceEvent args)
    {
        args.FlatModifier += entity.Comp.FailureModifier;

        if (HasLinkedOperatingComputer(entity))
            args.FlatModifier += entity.Comp.LinkedComputerFailureBonus;
    }

    /// <summary>
    /// Whether the disk in the slot of the computer has the protocol.
    /// </summary>
    public bool HasProtocol(Entity<OperatingComputerComponent> computer, ProtoId<SurgeryProtocolPrototype> protocol)
    {
        return TryGetDisk(computer, out var disk) && disk.Value.Comp.Protocols.Contains(protocol);
    }

    public bool TryGetDisk(Entity<OperatingComputerComponent> computer, [NotNullWhen(true)] out Entity<SurgeryDiskComponent>? disk)
    {
        disk = null;
        if (_itemSlots.GetItemOrNull(computer, computer.Comp.DiskSlotId) is not { } item
            || !TryComp<SurgeryDiskComponent>(item, out var diskComp))
            return false;

        disk = (item, diskComp);
        return true;
    }

    /// <summary>
    /// Checks whether the patient is anesthetized (sleeping, pain-numb from analgesics, or unconscious/dead).
    /// </summary>
    public bool IsPatientAnesthetized(EntityUid patient)
    {
        if (!TryComp<MobStateComponent>(patient, out var mobState) || _mobState.IsIncapacitated(patient, mobState))
            return true;

        if (HasComp<SleepingComponent>(patient))
            return true;

        if (_statusEffects.HasEffectComp<PainNumbnessStatusEffectComponent>(patient))
            return true;

        return false;
    }

    /// <summary>
    /// Checks whether the patient is buckled onto an <see cref="OperatingTableComponent"/>.
    /// </summary>
    public bool TryGetOperatingTable(EntityUid patient, out Entity<OperatingTableComponent> table)
    {
        table = default;

        if (!TryComp<BuckleComponent>(patient, out var buckle) ||
            !buckle.Buckled ||
            buckle.BuckledTo is not { } strapUid ||
            !TryComp<OperatingTableComponent>(strapUid, out var tableComp))
        {
            return false;
        }

        table = (strapUid, tableComp);
        return true;
    }

    private void OnOperatingTableInit(Entity<OperatingTableComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.ComputerPort);
    }

    private void OnOperatingComputerInit(Entity<OperatingComputerComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent.Owner, ent.Comp.TablePort);
    }

    private void OnOperatingComputerMapInit(Entity<OperatingComputerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(ent, out var source))
            return;

        foreach (var (sink, ports) in source.LinkedPorts)
        {
            if (!TryComp<OperatingTableComponent>(sink, out var table))
                continue;

            foreach (var (sourcePort, sinkPort) in ports)
            {
                if (sourcePort != ent.Comp.TablePort || sinkPort != table.ComputerPort)
                    continue;

                SetOperatingLink(ent, (sink, table));
                return;
            }
        }
    }

    private void OnOperatingComputerNewLink(Entity<OperatingComputerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner ||
            args.SourcePort != ent.Comp.TablePort ||
            !TryComp<OperatingTableComponent>(args.Sink, out var table) ||
            args.SinkPort != table.ComputerPort)
        {
            return;
        }

        if (ent.Comp.LinkedTable is { } prevTable && prevTable != args.Sink)
            _deviceLink.RemoveSinkFromSource(ent.Owner, prevTable);

        if (table.LinkedComputer is { } prevComputer && prevComputer != ent.Owner)
            _deviceLink.RemoveSinkFromSource(prevComputer, args.Sink);

        SetOperatingLink(ent, (args.Sink, table));
    }

    private void SetOperatingLink(Entity<OperatingComputerComponent> computer, Entity<OperatingTableComponent> table)
    {
        computer.Comp.LinkedTable = table.Owner;
        table.Comp.LinkedComputer = computer.Owner;
        Dirty(computer);
        Dirty(table);
    }

    private void OnOperatingTableAfterInteractUsing(Entity<OperatingTableComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !HasComp<NetworkConfiguratorComponent>(args.Used))
            return;

        args.Handled = true;
    }

    private void OnOperatingComputerPortDisconnected(Entity<OperatingComputerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.TablePort)
            return;

        if (ent.Comp.LinkedTable is { } tableUid &&
            TryComp<OperatingTableComponent>(tableUid, out var table) &&
            table.LinkedComputer == ent.Owner)
        {
            table.LinkedComputer = null;
            Dirty(tableUid, table);
        }

        ent.Comp.LinkedTable = null;
        Dirty(ent);
    }

    private void OnOperatingTablePortDisconnected(Entity<OperatingTableComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ent.Comp.ComputerPort)
            return;

        if (ent.Comp.LinkedComputer is { } computerUid &&
            TryComp<OperatingComputerComponent>(computerUid, out var computer) &&
            computer.LinkedTable == ent.Owner)
        {
            computer.LinkedTable = null;
            Dirty(computerUid, computer);
        }

        ent.Comp.LinkedComputer = null;
        Dirty(ent);
    }

    /// <summary>
    /// Checks whether the device link between <paramref name="computer"/> and <paramref name="table"/>
    /// is mutual, powered, and within <see cref="OperatingComputerComponent.MaxLinkDistance"/>.
    /// </summary>
    public bool IsOperatingLinkActive(Entity<OperatingComputerComponent> computer, Entity<OperatingTableComponent> table)
    {
        if (Deleted(computer.Owner) || Deleted(table.Owner))
            return false;

        if (computer.Comp.LinkedTable != table.Owner || table.Comp.LinkedComputer != computer.Owner)
            return false;

        if (!_powerReceiver.IsPowered(computer.Owner))
            return false;

        return _transform.InRange(
            Transform(computer.Owner).Coordinates,
            Transform(table.Owner).Coordinates,
            computer.Comp.MaxLinkDistance);
    }

    /// <summary>
    /// Checks whether an operating table has a linked, powered <see cref="OperatingComputerComponent"/> within range.
    /// </summary>
    public bool HasLinkedOperatingComputer(Entity<OperatingTableComponent> table, ProtoId<SurgeryProtocolPrototype>? protocol = null)
    {
        if (table.Comp.LinkedComputer is not { } computerUid ||
            !TryComp<OperatingComputerComponent>(computerUid, out var computerComp))
        {
            return false;
        }

        if (!IsOperatingLinkActive((computerUid, computerComp), table))
            return false;

        if (protocol is null)
            return true;

        return HasProtocol((computerUid, computerComp), protocol.Value);
    }

    /// <summary>
    /// Checks whether a patient (or operating table) has a linked <see cref="OperatingComputerComponent"/> in range.
    /// </summary>
    public bool HasLinkedOperatingComputer(EntityUid target, ProtoId<SurgeryProtocolPrototype>? protocol = null)
    {
        if (TryComp<OperatingTableComponent>(target, out var tableComp))
            return HasLinkedOperatingComputer((target, tableComp), protocol);

        if (TryGetOperatingTable(target, out var table))
            return HasLinkedOperatingComputer(table, protocol);

        return false;
    }

    /// <summary>
    /// Calculates the modified delay of a surgery edge taking into account surgeon skills, tool, and table.
    /// </summary>
    public float GetStepDelay(EntityUid patient, EntityUid surgeon, EntityUid? tool, SurgeryGraphEdge edge)
    {
        var baseDelay = SurgeryGraph.Delay(edge) ?? ErrorGettingDelayDelay;
        var skillEv = new GetSurgeryDelayModifiersEvent(0f, 1f);

        RaiseLocalEvent(surgeon, ref skillEv);

        var skillMultiplier = edge.DeceaseSkillBonus && skillEv.Multiplier < 1f
            ? (skillEv.Multiplier - 1f) / 2f + 1f
            : skillEv.Multiplier;

        var envEv = new GetSurgeryDelayModifiersEvent(0f, 1f);

        if (tool is { } toolUid)
            RaiseLocalEvent(toolUid, ref envEv);

        if (TryGetOperatingTable(patient, out var table))
            RaiseLocalEvent(table, ref envEv);
        else
            envEv.Multiplier *= _buckle.IsBuckled(patient) ? NoOperatingTableDelayMultiplier : FloorDelayMultiplier;

        if (IsImprovisedForEdge(tool, edge))
            envEv.Multiplier *= ImprovisedToolDelayMultiplier;

        var totalDelay = baseDelay * skillMultiplier * envEv.Multiplier + skillEv.FlatModifier + envEv.FlatModifier;
        return MathF.Max(0.5f, totalDelay);
    }

    /// <summary>
    /// Calculates the final probability [0..0.95] of failing a surgery step.
    /// </summary>
    public float GetStepFailureChance(EntityUid patient, EntityUid surgeon, EntityUid? tool, SurgeryGraphEdge edge)
    {
        var baseChance = SurgeryGraph.FailureChance(edge) ?? 0f;
        var failEv = new GetSurgeryFailureChanceEvent(baseChance, 1f);

        RaiseLocalEvent(surgeon, ref failEv);

        if (tool is { } toolUid)
            RaiseLocalEvent(toolUid, ref failEv);

        if (TryGetOperatingTable(patient, out var table))
            RaiseLocalEvent(table, ref failEv);
        else
            failEv.FlatModifier += _buckle.IsBuckled(patient) ? NoOperatingTableFailurePenalty : FloorFailurePenalty;

        if (IsImprovisedForEdge(tool, edge))
            failEv.FlatModifier += ImprovisedToolFailurePenalty;

        if (!IsPatientAnesthetized(patient) && !IsPainlessTool(tool) && !SurgeryGraph.IsPainless(edge))
            failEv.FlatModifier += UnanesthetizedFailurePenalty;

        var total = failEv.FlatModifier * failEv.Multiplier;
        return Math.Clamp(total, 0f, 0.95f);
    }

    /// <summary>
    /// Returns true when the tool being used does not cause patient pain or pain-related failure penalties.
    /// </summary>
    public bool IsPainlessTool(EntityUid? tool)
    {
        if (tool is not { } toolUid)
            return false;

        if (TryComp<SurgeryToolComponent>(toolUid, out var surgeryTool) && surgeryTool.IsPainless)
            return true;

        return HasComp<ImplantCheckInSurgeryComponent>(toolUid);
    }

    protected virtual void OnPatientPain(EntityUid patient)
    {
    }
}
