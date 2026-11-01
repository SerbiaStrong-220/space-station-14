using System.Linq;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.ModifyThreshold;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects;

public sealed partial class ModifyThresholdEffect : EntityEffect
{
    [DataField]
    public float Duration;

    [DataField(required: true)]
    public Dictionary<FixedPoint2, MobState> NewThresholds;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return string.Empty;
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EnsureComponent<ModifyThresholdComponent>(args.TargetEntity,
            out var modifyThresholdComponent);

        if (!args.EntityManager.TryGetComponent<MobThresholdsComponent>(args.TargetEntity, out var thresholdsComponent))
            return;

        modifyThresholdComponent.OldThresholds = thresholdsComponent.Thresholds.ToDictionary();
        modifyThresholdComponent.NewThresholds = NewThresholds;
        modifyThresholdComponent.Duration = Duration;
    }
}
