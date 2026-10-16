// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed partial class PullingSpeedModifierChangerSystem : SkillEntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<PullingSpeedModifierChangerComponent, ModifyPullingSpeed>(OnModifyPullingSpeed);
    }

    private void OnModifyPullingSpeed(Entity<PullingSpeedModifierChangerComponent> entity, ref ModifyPullingSpeed args)
    {
        var runSpeedPenalty = 1f - args.RunSpeedModifier;
        var walkSpeedPenalty = 1f - args.WalkSpeedModifier;

        if (runSpeedPenalty > 0)
        {
            args.RunSpeedModifier = 1f - GetChangedPenalty(entity, runSpeedPenalty);
        }

        if (walkSpeedPenalty > 0)
        {
            args.WalkSpeedModifier = 1f - GetChangedPenalty(entity, walkSpeedPenalty);
        }
    }

    private float GetChangedPenalty(Entity<PullingSpeedModifierChangerComponent> entity, float penalty)
    {
        return penalty <= entity.Comp.SpeedModifierToIgnore ? 0f : penalty * entity.Comp.SpeedPenaltyModifier;
    }
}

[ByRefEvent]
public record struct ModifyPullingSpeed(float WalkSpeedModifier, float RunSpeedModifier);
