// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.EntityEffects.Effects;

/// <summary>
/// </summary>
[UsedImplicitly]
public sealed partial class ChemicalAdaptationEffect : EventEntityEffect<ChemicalAdaptationEffect>
{
    /// <summary>
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Duration;

    /// <summary>
    /// </summary>
    [DataField(required: true)]
    public float Modifier;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-free-from-burden", ("chance", Probability));//ToDo_SS220 write smth here
    }
}

