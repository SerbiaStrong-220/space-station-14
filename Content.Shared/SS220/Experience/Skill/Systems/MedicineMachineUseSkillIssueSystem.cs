// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class MedicineMachineUseSkillIssueSystem : SkillEntitySystem
{
    private readonly ProtoId<SkillTreePrototype> _affectedSkillTree = "Medicine";
    private readonly FixedPoint4 _progressForDefibrillatorUse = 0.05;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<MedicineMachineUseSkillIssueComponent, GetHealthAnalyzerShuffleChance>(OnGetHealthAnalyzerShuffleChance);
        SubscribeEventToSkillEntity<MedicineMachineUseSkillIssueComponent, GetDefibrillatorUseChances>(OnGetDefibrillatorUseChances);
    }

    private void OnGetHealthAnalyzerShuffleChance(Entity<MedicineMachineUseSkillIssueComponent> entity, ref GetHealthAnalyzerShuffleChance args)
    {
        var newChance = ComputeNewChance(entity.Comp.HealthAnalyzerInfoShuffleChance, args.ShuffleChance);
        args.ShuffleChance = MathF.Max(newChance, 0f);
    }

    private void OnGetDefibrillatorUseChances(Entity<MedicineMachineUseSkillIssueComponent> entity, ref GetDefibrillatorUseChances args)
    {
        var newFailureChance = ComputeNewChance(entity.Comp.DefibrillatorFailureChance, args.FailureChance);
        var newSelfDamageChance = ComputeNewChance(entity.Comp.DefibrillatorSelfDamageChance, args.SelfDamageChance);

        args.FailureChance = MathF.Max(newFailureChance, 0f);
        args.SelfDamageChance = MathF.Max(newSelfDamageChance, 0f);

        TryChangeStudyingProgress(entity, _affectedSkillTree, _progressForDefibrillatorUse);
    }

    private float ComputeNewChance(float left, float right)
    {
        return (left + 1f) * (right + 1f) - 1f;
    }
}
