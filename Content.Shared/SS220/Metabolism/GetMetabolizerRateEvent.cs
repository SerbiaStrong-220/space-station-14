// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Metabolism;

/// <summary>
/// Raised on the body for each of its metabolizer organs when the metabolic multiplier is applied.
/// Unlike the global metabolic multiplier (stasis), it changes only the organs with the given stages.
/// </summary>
/// <param name="Stages">Metabolism stages of the organ.</param>
[ByRefEvent]
public record struct GetMetabolizerRateEvent(List<ProtoId<MetabolismStagePrototype>> Stages)
{
    /// <summary>
    /// How many times faster the organ metabolizes, the update interval is divided by it.
    /// </summary>
    public float Multiplier = 1f;
}
