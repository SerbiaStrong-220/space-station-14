// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.SS220.Hallucination;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class Hallucination : EntityEffectBase<Hallucination>
{
    [DataField(required: true)]
    public HallucinationSetting Setting = new();

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reaction-effect-guidebook-hallucination", ("duration", Setting.TotalDuration));
    }
}
