// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// Saving the multiplying modifier
/// </summary>
[UsedImplicitly]
public sealed partial class ChemicalAdaptationEffect : EntityEffectBase<ChemicalAdaptationEffect>
{
    /// <summary>
    /// How long will the modifier remain in effect
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Duration;

    /// <summary>
    /// "False" if you need to add Duration with each use of the effect, "true" if you need to refresh Duration
    /// </summary>
    [DataField]
    public bool Refresh = true;

    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-chemical-adaptation");
    }
}

