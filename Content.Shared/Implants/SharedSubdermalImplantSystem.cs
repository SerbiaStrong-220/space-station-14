using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.SS220.IgnoreLightVision;
using Content.Shared.SS220.MindSlave;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Implants;

public abstract partial class SharedSubdermalImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; //SS220-insert-currency-doafter
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!; //ss220 add freedom from bola
    [Dependency] private readonly TagSystem _tag = default!; // SS220-some-tag...

    private readonly ProtoId<TagPrototype> ThermalImplantTag = "ThermalImplant";

    public override void Initialize()
    {
        base.Initialize();
        InitializeRelay();

        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);

        SubscribeLocalEvent<SubdermalImplantComponent, UseChemicalImplantEvent>(OnChemicalImplant); // SS220 - chemical-implants start
        SubscribeLocalEvent<SubdermalImplantComponent, UseAdrenalImplantEvent>(OnAdrenalImplant); //ss220 add adrenal implant
        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaCopyImplantEvent>(OnDnaCopyImplant); //ss220 dna copy implant add
    }

    private void OnInsert(Entity<SubdermalImplantComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ImplanterComponent.ImplantSlotId)
            return;

        ent.Comp.ImplantedEntity = args.Container.Owner;
        Dirty(ent);

        EntityManager.AddComponents(ent.Comp.ImplantedEntity.Value, ent.Comp.ImplantComponents);
        if (ent.Comp.ImplantAction != null)
            _actions.AddAction(ent.Comp.ImplantedEntity.Value, ref ent.Comp.Action, ent.Comp.ImplantAction, ent.Owner);

        var ev = new ImplantImplantedEvent(ent.Owner, ent.Comp.ImplantedEntity.Value);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void OnRemoveAttempt(Entity<SubdermalImplantComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (ent.Comp.Permanent && ent.Comp.ImplantedEntity != null)
            args.Cancel();
    }

    private void OnRemove(Entity<SubdermalImplantComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // The results of the container change are already networked on their own
        if (_timing.ApplyingState)
            return;

        if (args.Container.ID != ImplanterComponent.ImplantSlotId)
            return;

        if (ent.Comp.ImplantedEntity == null || Terminating(ent.Comp.ImplantedEntity.Value))
            return;

        //SS220-mindslave start
        if (HasComp<MindSlaveImplantComponent>(ent))
        {
            var mindSlaveRemoved = new MindSlaveRemoved(ent, ent.Comp.ImplantedEntity);
            RaiseLocalEvent(ent, ref mindSlaveRemoved);
        }
        //SS220-mindslave end

        //SS220-removable-mindshield begin
        if (HasComp<MindShieldImplantComponent>(ent) && TryComp<MindShieldComponent>(ent.Comp.ImplantedEntity.Value, out var mindShield))
            RemComp(ent.Comp.ImplantedEntity.Value, mindShield);
        //SS220-removable-mindshield end
        //SS220 thermalvision begin
        if (_tag.HasTag(ent, ThermalImplantTag) && TryComp<ThermalVisionComponent>(ent.Comp.ImplantedEntity.Value, out var thermalVision))
            RemComp(ent.Comp.ImplantedEntity.Value, thermalVision);
        //SS220 thermalvision end

        EntityManager.RemoveComponents(ent.Comp.ImplantedEntity.Value, ent.Comp.ImplantComponents);
        _actions.RemoveAction(ent.Comp.ImplantedEntity.Value, ent.Comp.Action);
        ent.Comp.Action = null;

        var ev = new ImplantRemovedEvent(ent.Owner, ent.Comp.ImplantedEntity.Value);
        RaiseLocalEvent(ent.Owner, ref ev);

        ent.Comp.ImplantedEntity = null;
        Dirty(ent);
    }

    /// <summary>
    /// Add a list of implants to a person.
    /// Logs any implant ids that don't have <see cref="SubdermalImplantComponent"/>.
    /// </summary>
    public void AddImplants(EntityUid uid, IEnumerable<EntProtoId> implants)
    {
        foreach (var id in implants)
        {
            AddImplant(uid, id);
        }
    }

    /// <summary>
    /// Adds a single implant to a person, and returns the implant.
    /// Logs any implant ids that don't have <see cref="SubdermalImplantComponent"/>.
    /// </summary>
    /// <returns>
    /// The implant, if it was successfully created. Otherwise, null.
    /// </returns>>
    public EntityUid? AddImplant(EntityUid target, EntProtoId implantId)
    {
        if (_net.IsClient)
            return null; // can't interact with predicted spawns yet

        var coords = Transform(target).Coordinates;
        var implant = Spawn(implantId, coords);

        if (TryComp<SubdermalImplantComponent>(implant, out var implantComp))
        {
            ForceImplant(target, (implant, implantComp));
        }
        else
        {
            Log.Warning($"Tried to inject implant '{implantId}' without SubdermalImplantComponent into {ToPrettyString(target):implanted}");
            Del(implant);
            return null;
        }

        return implant;
    }

    /// <summary>
    /// Forces an implant into a person
    /// Good for on spawn related code or admin additions
    /// </summary>
    /// <param name="target">The entity to be implanted</param>
    /// <param name="implant"> The implant</param>
    public void ForceImplant(EntityUid target, Entity<SubdermalImplantComponent?> implant)
    {
        if (!Resolve(implant, ref implant.Comp))
            return;

        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);

        implant.Comp.ImplantedEntity = target;
        _container.Insert(implant.Owner, implantedComp.ImplantContainer);
    }

    /// <summary>
    /// Force remove a singular implant
    /// </summary>
    /// <param name="target">the implanted entity</param>
    /// <param name="implant">the implant</param>
    public void ForceRemove(Entity<ImplantedComponent?> target, EntityUid implant)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        _container.Remove(implant, target.Comp.ImplantContainer);
        PredictedQueueDel(implant);
    }

    /// <summary>
    /// Removes and deletes implants by force
    /// </summary>
    /// <param name="target">The entity to have implants removed</param>
    public void WipeImplants(Entity<ImplantedComponent?> target)
    {
        if (!Resolve(target, ref target.Comp, false))
            return;

        _container.CleanContainer(target.Comp.ImplantContainer);
    }
}

/// <summary>
/// Event that is raised whenever someone is implanted with any given implant.
/// Raised on the the implant entity.
/// </summary>
/// <remarks>
/// implant implant implant implant
/// </remarks>
[ByRefEvent]
public readonly record struct ImplantImplantedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid Implanted;

    public ImplantImplantedEvent(EntityUid implant, EntityUid implanted)
    {
        Implant = implant;
        Implanted = implanted;
    }
}

/// <summary>
/// Event that is raised whenever an implant is removed from someone.
/// Raised on the the implant entity.
/// </summary>

[ByRefEvent]
public readonly record struct ImplantRemovedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid Implanted;

    public ImplantRemovedEvent(EntityUid implant, EntityUid implanted)
    {
        Implant = implant;
        Implanted = implanted;
    }
}

//SS220-mindslave start
/// <summary>
/// Event raised whenever MindSlave implant is removed.
/// Raied on the implant itself.
/// </summary>
[ByRefEvent]
public readonly struct MindSlaveRemoved
{
    public readonly EntityUid Implant;
    public readonly EntityUid? Slave;

    public MindSlaveRemoved(EntityUid implant, EntityUid? slave)
    {
        Implant = implant;
        Slave = slave;
    }
}
//SS220-mindslave end
