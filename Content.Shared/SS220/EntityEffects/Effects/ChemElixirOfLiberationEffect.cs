// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Used when someone eats MiGoShroom
/// </summary>
[UsedImplicitly]
public sealed partial class ChemElixirOfLiberationEffect : EntityEffectBase<ChemElixirOfLiberationEffect>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-free-from-burden");
    }
}

