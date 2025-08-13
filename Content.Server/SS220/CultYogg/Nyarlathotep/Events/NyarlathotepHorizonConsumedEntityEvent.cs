// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Nyarlathotep;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

/// <summary>
/// Event raised on the entity being consumed whenever an Nyarlathotep horizon consumes an entity.
/// </summary>
[ByRefEvent]
public readonly record struct NyarlathotepHorizonConsumedEntityEvent (EntityUid Entity, EntityUid NyarlathotepHorizonUid, NyarlathotepHorizonComponent NyarlathotepHorizon)
{
    /// <summary>
    /// The entity that being consumed by the horizon.
    /// </summary>
    public readonly EntityUid Entity = Entity;

    /// <summary>
    /// The uid of the Nyarlathotep that consuming the entity.
    /// </summary>
    public readonly EntityUid NyarlathotepHorizonUid = NyarlathotepHorizonUid;

    /// <summary>
    /// The Nyarlathotep horizon that consuming the entity.
    /// </summary>
    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon = NyarlathotepHorizon;
}
