// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Containers;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

/// <summary>
/// Event raised on the entity being consumed whenever an Nyarlathotep horizon consumes an entity.
/// </summary>
[ByRefEvent]
public readonly record struct NyarlathotepHorizonConsumedEntityEvent
(EntityUid entity, EntityUid nyarlathotepHorizonUid, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? container)
{
    /// <summary>
    /// The entity that being consumed by the horizon.
    /// </summary>
    public readonly EntityUid Entity = entity;

    /// <summary>
    /// The uid of the Nyarlathotep that consuming the entity.
    /// </summary>
    public readonly EntityUid NyarlathotepHorizonUid = nyarlathotepHorizonUid;

    /// <summary>
    /// The Nyarlathotep horizon that consuming the entity.
    /// </summary>
    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon = nyarlathotepHorizon;

    /// <summary>
    /// The innermost container of the entity being consumed by the Nyarlathotep horizon that is not also in the process of being consumed by the event horizon.
    /// Used to correctly dump out the contents containers that are consumed by the event horizon.
    /// </summary>
    public readonly BaseContainer? Container = container;
}
