// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.SS220.CultYogg.Nyarlathotep.Events;
using Content.Server.Station.Components;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Singularity.Components;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.CultYogg.Nyarlathotep;
using Content.Shared.Tag;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;


namespace Content.Server.SS220.CultYogg.Nyarlathotep;

/// <summary>
/// The entity system primarily responsible for managing <see cref="NyarlathotepHorizonComponent"/>s.
/// Handles their consumption of entities.
/// </summary>
public sealed class NyarlathotepHorizonSystem : SharedNyarlathotepHorizonSystem
{
    #region Dependencies
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    #endregion Dependencies

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NyarlathotepHorizonComponent, StartCollideEvent>(OnStartCollide);

        SubscribeLocalEvent<MapGridComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<EventHorizonComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<GhostComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<StationDataComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsume);
        SubscribeLocalEvent<MindContainerComponent, NyarlathotepHorizonAttemptConsumeEntityEvent>(PreventConsumeMobs);
    }



    #region Event Handlers
    /// <summary>
    /// Handles horizons consuming any entities they bump into.
    /// </summary>
    private void OnStartCollide(Entity<NyarlathotepHorizonComponent> comp, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != comp.Comp.ColliderFixtureId)
            return;

        AttemptConsumeEntity(comp, args.OtherEntity);
    }

    /// <summary>
    /// Prevents a Nyarlathotep from colliding with anything it is incapable of consuming.
    /// </summary>
    protected override bool PreventCollide(Entity<NyarlathotepHorizonComponent> comp, ref PreventCollideEvent args)
    {
        if (base.PreventCollide(comp, ref args) || args.Cancelled)
            return true;

        args.Cancelled = args.OurFixture.Hard && CanConsumeEntity(comp, args.OtherEntity);
        return false;
    }

    /// <summary>
    /// An event handler that prevents Nyarlathotep from consuming living entities, instead we just deal damage to them.
    /// This is also the logic for preventing MiGo damage.
    /// </summary>
    private void PreventConsumeMobs(Entity<MindContainerComponent> comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        PreventConsume(comp.Owner, comp.Comp, ref args);
        if (_mob.IsAlive(args.entity) && !HasComp<MiGoComponent>(args.entity))
            _bodySystem.GibBody(comp.Owner);
    }

    /// <summary>
    /// A generic event handler that prevents Nyarlathotep from consuming entities with a component of a given type if registered.
    /// </summary>
    private static void PreventConsume<TComp>(EntityUid uid, TComp comp, ref NyarlathotepHorizonAttemptConsumeEntityEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = true;
    }
    #endregion Event Handlers

    /// <summary>
    /// Checks whether a horizon can consume a given entity.
    /// </summary>
    private bool CanConsumeEntity(Entity<NyarlathotepHorizonComponent> nyarlathotep, EntityUid entityToConsume)
    {
        var ev = new NyarlathotepHorizonAttemptConsumeEntityEvent(entityToConsume, nyarlathotep.Owner, nyarlathotep.Comp);
        RaiseLocalEvent(entityToConsume, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Makes a horizon consume a given entity.
    /// </summary>
    private void ConsumeEntity(Entity<NyarlathotepHorizonComponent> nyarlathotep,
        EntityUid entityToConsume)
    {
        if (!EntityManager.IsQueuedForDeletion(entityToConsume)
            && (HasComp<MindContainerComponent>(entityToConsume)
            || _tagSystem.HasTag(entityToConsume, "HighRiskItem")))
        {
            _adminLogger.Add(LogType.EntityDelete, LogImpact.Extreme, $"{ToPrettyString(entityToConsume)} entered the event horizon of {ToPrettyString(nyarlathotep)} and was deleted");
        }

        EntityManager.QueueDeleteEntity(entityToConsume);
        var evEaten = new NyarlathotepHorizonConsumedEntityEvent(
                            entityToConsume,
                            nyarlathotep,
                            nyarlathotep.Comp);
        RaiseLocalEvent(entityToConsume, ref evEaten);
    }

    /// <summary>
    /// Makes an event horizon attempt to consume a given entity.
    /// </summary>
    private void AttemptConsumeEntity(Entity<NyarlathotepHorizonComponent> nyarlathotep,
            EntityUid entityToConsume)
    {
        if (!CanConsumeEntity(nyarlathotep, entityToConsume))
            return;

        ConsumeEntity(nyarlathotep, entityToConsume);
    }
}
