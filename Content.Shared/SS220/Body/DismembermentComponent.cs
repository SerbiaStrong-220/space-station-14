// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
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
    /// Tick in which a part was already torn off or an explosion rolled its own tearing.
    /// The damage that follows in this tick must not roll another tear.
    /// </summary>
    [ViewVariables]
    public GameTick TearTick = GameTick.Zero;
}
