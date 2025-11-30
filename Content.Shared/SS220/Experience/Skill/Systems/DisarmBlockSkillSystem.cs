// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions.Events;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmBlockSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<DisarmBlockSkillComponent, DisarmAttemptEvent>();

        SubscribeLocalEvent<DisarmBlockSkillComponent, DisarmAttemptEvent>(OnDisarmAttempt);
    }

    private void OnDisarmAttempt(Entity<DisarmBlockSkillComponent> entity, ref DisarmAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
