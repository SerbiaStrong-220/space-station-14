// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Random;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Body;

/// <summary>
/// A body that loses its <see cref="DismemberableComponent"/> parts to strong hits and explosions.
/// </summary>
[RegisterComponent]
public sealed partial class DismembermentComponent : Component
{
    /// <summary>
    /// Organ categories a hit lands on, weighted like the traumas. Categories without a dismemberable organ only take their share of hits.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> HitZones = "DismembermentZone";

    /// <summary>
    /// Chance of a hit (its cuts and blows) to tear off a part with a severe trauma, if the part has no own <see cref="DismemberableComponent.HitRoll"/>.
    /// Null - hits tear nothing off.
    /// </summary>
    [DataField]
    public TraumaRoll? HitRoll;

    /// <summary>
    /// Chance of an explosion (its blunt damage after armor) to tear off each part with <see cref="DismemberableComponent.TornByBlast"/>.
    /// Null - explosions tear nothing off.
    /// </summary>
    [DataField]
    public TraumaRoll? BlastRoll;

    /// <summary>
    /// Damage of the wound left by a torn off part.
    /// </summary>
    [DataField]
    public DamageSpecifier? TearDamage;

    /// <summary>
    /// A torn off part flies away by a random distance between these.
    /// </summary>
    [DataField(required: true)]
    public float TearThrowMinDistance;

    [DataField(required: true)]
    public float TearThrowMaxDistance;

    /// <summary>
    /// Tick in which a part was already torn off or an explosion rolled its own tearing.
    /// The damage that follows in this tick must not roll another tear.
    /// </summary>
    [ViewVariables]
    public GameTick TearTick = GameTick.Zero;
}
