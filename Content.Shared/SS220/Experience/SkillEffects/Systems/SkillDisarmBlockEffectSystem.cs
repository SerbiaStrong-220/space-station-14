// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions.Events;
using Content.Shared.SS220.Experience.SkillEffects.Components;
using Content.Shared.SS220.Experience.Systems;

namespace Content.Shared.SS220.Experience.SkillEffects.Systems;

public sealed class SkillDisarmBlockEffectSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<SkillDisarmBlockEffectComponent, DisarmAttemptEvent>();

        SubscribeLocalEvent<SkillDisarmBlockEffectComponent, DisarmAttemptEvent>(OnDisarmAttempt);
    }

    private void OnDisarmAttempt(Entity<SkillDisarmBlockEffectComponent> entity, ref DisarmAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
