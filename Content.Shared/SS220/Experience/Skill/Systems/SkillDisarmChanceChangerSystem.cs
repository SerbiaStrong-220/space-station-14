// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmChanceChangerSkillSystem : SkillEntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmerMultiplierEvent>(OnDisarmDisarmerAttempt);
        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceTargetMultiplierEvent>(OnDisarmAttempt);
    }

    private void OnDisarmDisarmerAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmerMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmByMultiplier;
    }

    private void OnDisarmAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceTargetMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmedMultiplier;
    }
}
