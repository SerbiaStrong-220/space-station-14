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

        _experience.RelayEventToSkillEntity<DisarmChanceChangerSkillComponent, GetDisarmChanceMultiplierEvent>();

        SubscribeLocalEvent<DisarmChanceChangerSkillComponent, GetDisarmChanceMultiplierEvent>(OnDisarmAttempt);
    }

    private void OnDisarmAttempt(Entity<DisarmChanceChangerSkillComponent> entity, ref GetDisarmChanceMultiplierEvent args)
    {
        args.Multiplier *= entity.Comp.Multiplier;
    }
}
