// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class GrantLanguageSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantLanguageComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<GrantLanguageComponent> entity, ref MapInitEvent _)
    {
        if (!_experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
        {
            Log.Error($"Cant get owner of skill entity {ToPrettyString(entity)}");
            return;
        }

        if (!TryComp<LanguageComponent>(experienceEntity, out var languageComponent))
        {
            Log.Warning($"Cant resolve {nameof(LanguageComponent)} entity {ToPrettyString(experienceEntity)} while trying to add it language by skill");
            return;
        }

        _language.AddLanguages((experienceEntity.Value, languageComponent), entity.Comp.Languages);
    }
}
