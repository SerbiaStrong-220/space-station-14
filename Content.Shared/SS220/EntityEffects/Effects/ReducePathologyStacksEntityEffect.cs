// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Removes a stack of a pathology while it is at its first level,
/// e.g. medicine treats a damaged organ, but a failed one needs surgery. Never adds the pathology.
/// </summary>
public sealed partial class ReducePathologyStacks : EntityEffectBase<ReducePathologyStacks>
{
    [DataField(required: true)]
    public ProtoId<PathologyPrototype> Pathology;
}
