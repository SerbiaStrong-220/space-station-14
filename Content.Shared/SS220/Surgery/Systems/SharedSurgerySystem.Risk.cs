// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.NightVision;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Systems;

public abstract partial class SharedSurgerySystem
{
    [Dependency] private SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const float UnanesthetizedFailurePenalty = 0.25f;
    private const float NoOperatingTableFailurePenalty = 0.20f;
    private const float NoOperatingTableDelayMultiplier = 1.35f;
    private const float PoorLightingFailurePenalty = 0.15f;
    private const float LightCheckRange = 3.5f;

    private readonly HashSet<EntityUid> _lightEntityBuffer = new();

    private void InitializeRisk()
    {
        SubscribeLocalEvent<SurgeryToolComponent, GetSurgeryDelayModifiersEvent>(OnToolGetDelayModifiers);
        SubscribeLocalEvent<SurgeryToolComponent, GetSurgeryFailureChanceEvent>(OnToolGetFailureChance);
        SubscribeLocalEvent<OperatingTableComponent, ComponentInit>(OnOperatingTableInit);
        SubscribeLocalEvent<OperatingTableComponent, PortDisconnectedEvent>(OnOperatingTablePortDisconnected);
        SubscribeLocalEvent<OperatingTableComponent, GetSurgeryDelayModifiersEvent>(OnTableGetDelayModifiers);
        SubscribeLocalEvent<OperatingTableComponent, GetSurgeryFailureChanceEvent>(OnTableGetFailureChance);
        SubscribeLocalEvent<OperatingComputerComponent, ComponentInit>(OnOperatingComputerInit);
        SubscribeLocalEvent<OperatingComputerComponent, NewLinkEvent>(OnOperatingComputerNewLink);
        SubscribeLocalEvent<OperatingComputerComponent, PortDisconnectedEvent>(OnOperatingComputerPortDisconnected);
        SubscribeLocalEvent<OperatingComputerComponent, EntInsertedIntoContainerMessage>(OnComputerSlotInserted);
        SubscribeLocalEvent<OperatingComputerComponent, EntRemovedFromContainerMessage>(OnComputerSlotRemoved);
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

    private void OnComputerSlotInserted(Entity<OperatingComputerComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != entity.Comp.DiskSlotId)
            return;

        if (!TryComp<SurgeryDiskComponent>(args.Entity, out var disk))
            return;

        foreach (var surgery in disk.Surgeries)
        {
            entity.Comp.UnlockedSurgeries.Add(surgery);
        }

        Dirty(entity);
    }

    private void OnComputerSlotRemoved(Entity<OperatingComputerComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != entity.Comp.DiskSlotId)
            return;

        if (!TryComp<SurgeryDiskComponent>(args.Entity, out var disk))
            return;

        foreach (var surgery in disk.Surgeries)
        {
            entity.Comp.UnlockedSurgeries.Remove(surgery);
        }

        Dirty(entity);
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
    public bool IsOnOperatingTable(EntityUid patient, out Entity<OperatingTableComponent> table)
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

        ent.Comp.LinkedTable = args.Sink;
        table.LinkedComputer = ent.Owner;
        Dirty(ent);
        Dirty(args.Sink, table);
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
    public bool HasLinkedOperatingComputer(Entity<OperatingTableComponent> table, ProtoId<SurgeryGraphPrototype>? requiredSurgery = null)
    {
        if (table.Comp.LinkedComputer is not { } computerUid ||
            !TryComp<OperatingComputerComponent>(computerUid, out var computerComp))
        {
            return false;
        }

        if (!IsOperatingLinkActive((computerUid, computerComp), table))
            return false;

        if (requiredSurgery is null)
            return true;

        return computerComp.UnlockedSurgeries.Contains(requiredSurgery.Value);
    }

    /// <summary>
    /// Checks whether a patient (or operating table) has a linked <see cref="OperatingComputerComponent"/> in range.
    /// </summary>
    public bool HasLinkedOperatingComputer(EntityUid target, ProtoId<SurgeryGraphPrototype>? requiredSurgery = null)
    {
        if (TryComp<OperatingTableComponent>(target, out var tableComp))
            return HasLinkedOperatingComputer((target, tableComp), requiredSurgery);

        if (IsOnOperatingTable(target, out var table))
            return HasLinkedOperatingComputer(table, requiredSurgery);

        return false;
    }

    /// <summary>
    /// Checks whether the surgeon has sufficient light (either active NightVision or an enabled unoccluded point light nearby).
    /// </summary>
    public bool HasSufficientLight(EntityUid patient, EntityUid surgeon)
    {
        var xform = Transform(patient);
        if (xform.MapID == MapId.Nullspace)
            return true;

        var nvQuery = EntityQueryEnumerator<NightVisionComponent>();
        while (nvQuery.MoveNext(out var nv))
        {
            if (nv.Enabled && nv.Wearer == surgeon)
                return true;
        }

        _lightEntityBuffer.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, LightCheckRange, _lightEntityBuffer, LookupFlags.All);

        foreach (var uid in _lightEntityBuffer)
        {
            if (_pointLight.TryGetLight(uid, out var light) && light.Enabled && !light.ContainerOccluded)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the modified delay of a surgery edge taking into account surgeon skills, tool, and table.
    /// </summary>
    public float GetStepDelay(EntityUid patient, EntityUid surgeon, EntityUid? tool, SurgeryGraphEdge edge)
    {
        var baseDelay = SurgeryGraph.Delay(edge) ?? ErrorGettingDelayDelay;
        var skillEv = new GetSurgeryDelayModifiersEvent(0f, 1f);

        RaiseLocalEvent(patient, ref skillEv);
        RaiseLocalEvent(surgeon, ref skillEv);

        var skillMultiplier = edge.DeceaseSkillBonus && skillEv.Multiplier > 1f
            ? (skillEv.Multiplier - 1f) / 2f + 1f
            : skillEv.Multiplier;

        var envEv = new GetSurgeryDelayModifiersEvent(0f, 1f);

        if (tool is { } toolUid)
            RaiseLocalEvent(toolUid, ref envEv);

        if (IsOnOperatingTable(patient, out var table))
            RaiseLocalEvent(table, ref envEv);
        else if (HasComp<BuckleComponent>(patient))
            envEv.Multiplier *= NoOperatingTableDelayMultiplier;

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

        if (IsOnOperatingTable(patient, out var table))
            RaiseLocalEvent(table, ref failEv);
        else if (HasComp<BuckleComponent>(patient))
            failEv.FlatModifier += NoOperatingTableFailurePenalty;

        if (!IsPatientAnesthetized(patient))
            failEv.FlatModifier += UnanesthetizedFailurePenalty;

        if (!HasSufficientLight(patient, surgeon))
            failEv.FlatModifier += PoorLightingFailurePenalty;

        var total = failEv.FlatModifier * failEv.Multiplier;
        return Math.Clamp(total, 0f, 0.95f);
    }

    protected virtual void OnPatientPain(EntityUid patient)
    {
    }
}
