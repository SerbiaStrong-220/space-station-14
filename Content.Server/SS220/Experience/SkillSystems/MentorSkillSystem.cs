// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Experience.SkillSystems;

public sealed class MentorSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<MentorSkillComponent, EntitySpokeEvent>();

        SubscribeLocalEvent<MentorSkillComponent, EntitySpokeEvent>(OnSpoke);
    }

    private void OnSpoke(Entity<MentorSkillComponent> entity, ref EntitySpokeEvent args)
    {
        if (_gameTiming.CurTime < entity.Comp.LastActivate + entity.Comp.ActivateTimeout)
            return;

        entity.Comp.LastActivate = _gameTiming.CurTime;

        var ownerEntity = Transform(entity).ParentUid;
        var experienceEntities = _entityLookup.GetEntitiesInRange<ExperienceComponent>(Transform(ownerEntity).Coordinates, entity.Comp.Range);

        foreach (var experienceEntity in experienceEntities)
        {
            var affectedComp = EnsureComp<AffectedByMentorComponent>(entity);

            foreach (var (skillTreeId, info) in entity.Comp.TeachInfo)
            {
                if (affectedComp.TeachInfo.TryGetValue(skillTreeId, out var oldInfo) && info < oldInfo)
                    continue;

                affectedComp.TeachInfo[skillTreeId] = info;
            }
        }
    }
}
