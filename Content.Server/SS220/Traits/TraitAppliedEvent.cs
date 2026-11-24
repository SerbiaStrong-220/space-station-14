// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Traits;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Traits;

/// <summary>
/// Raised on a spawned character for each lobby trait the trait system gave it, after the components of the trait are added.
/// </summary>
[ByRefEvent]
public readonly record struct TraitAppliedEvent(ProtoId<TraitPrototype> Trait);
