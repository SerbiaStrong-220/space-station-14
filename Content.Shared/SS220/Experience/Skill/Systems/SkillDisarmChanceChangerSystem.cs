// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmChanceChangerSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmerMultiplierEvent>();
        _experience.RelayEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmedMultiplierEvent>();

        SubscribeLocalEvent<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmerMultiplierEvent>(OnDisarmDisarmedAttempt);
        SubscribeLocalEvent<DisarmChanceChangerSkillComponent, GetDisarmChanceDisarmedMultiplierEvent>(OnDisarmDisarmerAttempt);
    }

    private void OnDisarmDisarmedAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmerMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmedMultiplier;
    }

    private void OnDisarmDisarmerAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceDisarmedMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.DisarmByMultiplier;
    }
}
