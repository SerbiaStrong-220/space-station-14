// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Used when someone eats MiGoShroom
/// </summary>
[UsedImplicitly]
public sealed partial class ChemElixirOfLiberationEffect : EventEntityEffect<ChemElixirOfLiberationEffect>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-ss220-free-from-burden", ("chance", Probability));
}

