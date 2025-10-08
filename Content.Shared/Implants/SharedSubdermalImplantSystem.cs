using System.Linq;
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
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public abstract partial class SharedSubdermalImplantSystem : EntitySystem // SS220 move out code into partial
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; //SS220-insert-currency-doafter
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!; //ss220 add freedom from bola

    public const string BaseStorageId = "storagebase";

    //SS220 thermalvision begin
    public const string ThermalImplantTag = "ThermalImplant";
    //SS220 thermalvision end

    private static readonly ProtoId<TagPrototype> MicroBombTag = "MicroBomb";
    private static readonly ProtoId<TagPrototype> MacroBombTag = "MacroBomb";

    public override void Initialize()
    {
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SubdermalImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<SubdermalImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, AfterInteractUsingEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SuicideEvent>(RelayToImplantEvent);

        SubscribeLocalEvent<SubdermalImplantComponent, UseChemicalImplantEvent>(OnChemicalImplant); // SS220 - chemical-implants start
        SubscribeLocalEvent<SubdermalImplantComponent, UseAdrenalImplantEvent>(OnAdrenalImplant); //ss220 add adrenal implant
        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaCopyImplantEvent>(OnDnaCopyImplant); //ss220 dna copy implant add
    }

    private void OnInsert(EntityUid uid, SubdermalImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (component.ImplantedEntity == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.ImplantAction))
        {
            _actionsSystem.AddAction(component.ImplantedEntity.Value, ref component.Action, component.ImplantAction, uid);
        }

        // replace micro bomb with macro bomb
        // TODO: this shouldn't be hardcoded here
        if (_container.TryGetContainer(component.ImplantedEntity.Value, ImplanterComponent.ImplantSlotId, out var implantContainer) && _tag.HasTag(uid, MacroBombTag))
        {
            foreach (var implant in implantContainer.ContainedEntities)
            {
                if (_tag.HasTag(implant, MicroBombTag))
                {
                    _container.Remove(implant, implantContainer);
                    PredictedQueueDel(implant);
                }
            }
        }

        var ev = new ImplantImplantedEvent(uid, component.ImplantedEntity.Value);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnRemoveAttempt(EntityUid uid, SubdermalImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }

    private void OnRemove(EntityUid uid, SubdermalImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.ImplantedEntity == null || Terminating(component.ImplantedEntity.Value))
            return;

        if (component.ImplantAction != null)
            _actionsSystem.RemoveProvidedActions(component.ImplantedEntity.Value, uid);

        //SS220-mindslave start
        if (HasComp<MindSlaveImplantComponent>(uid))
        {
            var mindSlaveRemoved = new MindSlaveRemoved(uid, component.ImplantedEntity);
            RaiseLocalEvent(uid, ref mindSlaveRemoved);
        }
        //SS220-mindslave end

        //SS220-removable-mindshield begin
        if (HasComp<MindShieldImplantComponent>(uid) && TryComp<MindShieldComponent>(component.ImplantedEntity.Value, out var mindShield))
            RemComp(component.ImplantedEntity.Value, mindShield);
        //SS220-removable-mindshield end
        //SS220 thermalvision begin
        if (_tag.HasTag(uid, ThermalImplantTag) && TryComp<ThermalVisionComponent>(component.ImplantedEntity.Value, out var thermalVision))
            RemComp(component.ImplantedEntity.Value, thermalVision);
        //SS220 thermalvision end
        if (!_container.TryGetContainer(uid, BaseStorageId, out var storageImplant))
            return;

        var containedEntites = storageImplant.ContainedEntities.ToArray();

        foreach (var entity in containedEntites)
        {
            _transformSystem.DropNextTo(entity, uid);
        }
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
    public EntityUid? AddImplant(EntityUid uid, String implantId)
    {
        var coords = Transform(uid).Coordinates;
        var ent = Spawn(implantId, coords);

        if (TryComp<SubdermalImplantComponent>(ent, out var implant))
        {
            ForceImplant(uid, ent, implant);
        }
        else
        {
            Log.Warning($"Found invalid starting implant '{implantId}' on {uid} {ToPrettyString(uid):implanted}");
            Del(ent);
            return null;
        }

        return ent;
    }

    /// <summary>
    /// Forces an implant into a person
    /// Good for on spawn related code or admin additions
    /// </summary>
    /// <param name="target">The entity to be implanted</param>
    /// <param name="implant"> The implant</param>
    /// <param name="component">The implant component</param>
    public void ForceImplant(EntityUid target, EntityUid implant, SubdermalImplantComponent component)
    {
        //If the target doesn't have the implanted component, add it.
        var implantedComp = EnsureComp<ImplantedComponent>(target);
        var implantContainer = implantedComp.ImplantContainer;

        component.ImplantedEntity = target;
        _container.Insert(implant, implantContainer);
    }

    /// <summary>
    /// Force remove a singular implant
    /// </summary>
    /// <param name="target">the implanted entity</param>
    /// <param name="implant">the implant</param>
    [PublicAPI]
    public void ForceRemove(EntityUid target, EntityUid implant)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        _container.Remove(implant, implantContainer);
        QueueDel(implant);
    }

    /// <summary>
    /// Removes and deletes implants by force
    /// </summary>
    /// <param name="target">The entity to have implants removed</param>
    [PublicAPI]
    public void WipeImplants(EntityUid target)
    {
        if (!TryComp<ImplantedComponent>(target, out var implanted))
            return;

        var implantContainer = implanted.ImplantContainer;

        _container.CleanContainer(implantContainer);
    }

    //Relays from the implanted to the implant
    private void RelayToImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T : notnull
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var relayEv = new ImplantRelayEvent<T>(args, uid);
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (args is HandledEntityEventArgs { Handled : true })
                return;

            RaiseLocalEvent(implant, relayEv);
        }
    }
}

public sealed class ImplantRelayEvent<T> where T : notnull
{
    public readonly T Event;

    public readonly EntityUid ImplantedEntity;

    public ImplantRelayEvent(T ev, EntityUid implantedEntity)
    {
        Event = ev;
        ImplantedEntity = implantedEntity;
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
public readonly struct ImplantImplantedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid? Implanted;

    public ImplantImplantedEvent(EntityUid implant, EntityUid? implanted)
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
