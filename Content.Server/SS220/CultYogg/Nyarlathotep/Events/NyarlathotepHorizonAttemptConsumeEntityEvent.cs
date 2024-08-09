// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

/// <summary>
/// Event raised on the target entity whenever an Nyarlathotep horizon attempts to consume an entity.
/// Can be cancelled to prevent the target entity from being consumed.
/// </summary>
[ByRefEvent]
public record struct NyarlathotepHorizonAttemptConsumeEntityEvent
    (EntityUid entity, EntityUid nyarlathotepHorizonUid, NyarlathotepHorizonComponent nyarlathotepHorizon)
{
    /// <summary>
    /// The entity that the Nyarlathotep horizon is attempting to consume.
    /// </summary>
    public readonly EntityUid Entity = entity;

    /// <summary>
    /// The uid of the Nyarlathotep consuming the entity.
    /// </summary>
    public readonly EntityUid NyarlathotepHorizonUid = nyarlathotepHorizonUid;

    /// <summary>
    /// The Nyarlathotep horizon consuming the target entity.
    /// </summary>
    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon = nyarlathotepHorizon;

    /// <summary>
    /// Whether the Nyarlathotep has been prevented from consuming the target entity.
    /// </summary>
    public bool Cancelled = false;
}
