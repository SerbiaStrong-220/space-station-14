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

    /// <summary>
    /// This acts as "average" level, so every mob without experience component will have this level
    /// </summary>
    private readonly int _baseSkillLevel = 2;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmerMultiplierEvent>(OnDisarmDisarmedAttempt);
        SubscribeEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmedMultiplierEvent>(OnDisarmDisarmerAttempt);
    }

    private void OnDisarmDisarmedAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmerMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmByMultiplier;

        if (ValidTargetForProgressing(args.Disarmer, args.Disarmed))
            TryChangeStudyingProgress(entity, _affectedSkillTree, _progressForDisarming);
    }

    private void OnDisarmDisarmerAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmedMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmedMultiplier;

        // swap parameters to match logic of progression
        if (ValidTargetForProgressing(args.Disarmed, args.Disarmer))
            TryChangeStudyingProgress(entity, _affectedSkillTree, _progressForDisarming);
    }

    private bool ValidTargetForProgressing(EntityUid performerUid, EntityUid targetUid)
    {
        Experience.TryGetSkillTreeLevel(performerUid, _affectedSkillTree, out var performerLevel);
        Experience.TryGetSkillTreeLevel(targetUid, _affectedSkillTree, out var targetLevel);

        return (performerLevel ?? _baseSkillLevel) < (targetLevel ?? _baseSkillLevel);
    }
}
