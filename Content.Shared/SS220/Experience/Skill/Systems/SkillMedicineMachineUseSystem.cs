// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class MedicineMachineUseSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<MedicineMachineUseSkillComponent, GetHealthAnalyzerShuffleChance>();
        _experience.RelayEventToSkillEntity<MedicineMachineUseSkillComponent, GetDefibrillatorUseChances>();

        SubscribeLocalEvent<MedicineMachineUseSkillComponent, GetHealthAnalyzerShuffleChance>(OnGetHealthAnalyzerShuffleChance);
        SubscribeLocalEvent<MedicineMachineUseSkillComponent, GetDefibrillatorUseChances>(OnGetDefibrillatorUseChances);
    }

    private void OnGetHealthAnalyzerShuffleChance(Entity<MedicineMachineUseSkillComponent> entity, ref GetHealthAnalyzerShuffleChance args)
    {
        var newChance = ComputeNewChance(entity.Comp.HealthAnalyzerInfoShuffleChance, args.ShuffleChance);
        args.ShuffleChance = MathF.Max(newChance, 0f);
    }

    private void OnGetDefibrillatorUseChances(Entity<MedicineMachineUseSkillComponent> entity, ref GetDefibrillatorUseChances args)
    {
        var newFailureChance = ComputeNewChance(entity.Comp.DefibrillatorFailureChance, args.FailureChance);
        var newSelfDamageChance = ComputeNewChance(entity.Comp.DefibrillatorSelfDamageChance, args.SelfDamageChance);

        args.FailureChance = MathF.Max(newFailureChance, 0f);
        args.SelfDamageChance = MathF.Max(newSelfDamageChance, 0f);
    }

    private float ComputeNewChance(float left, float right)
    {
        return (left + 1f) * (right + 1f) - 1f;
    }
}
