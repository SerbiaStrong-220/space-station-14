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
        if (args.RunSpeedModifier < 1f)
        {
            args.RunSpeedModifier = ChangeModifier(entity, args.RunSpeedModifier);
        }

        if (args.WalkSpeedModifier < 1f)
        {
            args.WalkSpeedModifier = ChangeModifier(entity, args.WalkSpeedModifier);
        }
    }

    private float ChangeModifier(Entity<PullingSpeedModifierChangerComponent> entity, float modifier)
    {
        return modifier >= 1f - entity.Comp.SpeedModifierToIgnore ? 1f : 1f - (1f - modifier) * entity.Comp.SpeedPenaltyModifier;
    }
}

[ByRefEvent]
public record struct ModifyPullingSpeed(float WalkSpeedModifier, float RunSpeedModifier);
