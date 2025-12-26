// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class GrantLanguageSkillSystem : SkillEntitySystem
{
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantLanguageSkillComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GrantLanguageSkillComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<GrantLanguageSkillComponent> entity, ref MapInitEvent _)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
            return;

        if (!TryComp<LanguageComponent>(experienceEntity, out var languageComponent))
        {
            Log.Warning($"Cant resolve {nameof(LanguageComponent)} entity {ToPrettyString(experienceEntity)} while trying to add it language by skill");
            return;
        }

        _language.AddLanguages((experienceEntity.Value, languageComponent), entity.Comp.Languages);
        TryAddToAdminLogs(entity, $"granted languages to skill owner, languages: {string.Join('|', entity.Comp.Languages)}");
    }

    private void OnRemove(Entity<GrantLanguageSkillComponent> entity, ref ComponentRemove _)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
            return;

        if (!TryComp<LanguageComponent>(experienceEntity, out var languageComponent))
        {
            Log.Warning($"Cant resolve {nameof(LanguageComponent)} entity {ToPrettyString(experienceEntity)} while trying to remove it language by skill");
            return;
        }

        foreach (var language in entity.Comp.Languages)
        {
            _language.RemoveLanguage((experienceEntity.Value, languageComponent), language.Id);
        }

        TryAddToAdminLogs(entity, $"removed languages from skill owner, languages: {string.Join('|', entity.Comp.Languages)}");
    }
}
