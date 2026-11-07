using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Reagent;

public sealed partial class SaintWaterFlammableEffect : EntityEffectBase<SaintWaterFlammableEffect>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Помогает бороться с нечистью";
    }
}
