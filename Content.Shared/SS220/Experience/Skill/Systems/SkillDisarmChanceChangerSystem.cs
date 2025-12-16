// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmChanceChangerSkillSystem : SkillEntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    private readonly ProtoId<SkillTreePrototype> _affectedSkillTree = "CombatTraining";
    private readonly FixedPoint4 _progressForDisarming = 0.03;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmerMultiplierEvent>(OnDisarmDisarmedAttempt);
        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmedMultiplierEvent>(OnDisarmDisarmerAttempt);
    }

    private void OnDisarmDisarmedAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmerMultiplierEvent args)
    {
        args.Multiplier /= entity.Comp.DisarmByMultiplier;

        TryChangeStudyingProgress(entity, _affectedSkillTree, _progressForDisarming);
    }

    private void OnDisarmDisarmerAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmedMultiplierEvent args)
    {
        args.Multiplier /= entity.Comp.DisarmedMultiplier;

        TryChangeStudyingProgress(entity, _affectedSkillTree, _progressForDisarming);
    }
}
