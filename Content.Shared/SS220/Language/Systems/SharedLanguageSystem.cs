// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Ghost;
using Content.Shared.SS220.Language.Components;

namespace Content.Shared.SS220.Language.Systems;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly LanguageManager _language = default!;

    public readonly string UniversalLanguage = "Universal";
    public readonly string GalacticLanguage = "Galactic";

    #region Component
    /// <summary>
    /// Adds languages to <see cref="LanguageComponent.AvailableLanguages"/> from <paramref name="languageIds"/>.
    /// </summary>
    /// <param name="canSpeak">Will entity be able to speak this language</param>
    public void AddLanguages(Entity<LanguageComponent> ent, IEnumerable<string> languageIds, bool canSpeak = false)
    {
        foreach (var language in languageIds)
        {
            AddLanguage(ent, language, canSpeak);
        }
    }

    /// <summary>
    /// Adds a <see cref="LanguageDefinition"/> from list to <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void AddLanguages(Entity<LanguageComponent> ent, List<LanguageDefinition> definitions)
    {
        foreach (var def in definitions)
        {
            AddLanguage(ent, def);
        }
    }

    /// <summary>
    /// Adds language to the <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <param name="canSpeak">Will entity be able to speak this language</param>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful added</returns>
    public bool AddLanguage(Entity<LanguageComponent> ent, string languageId, bool canSpeak = false)
    {
        if (GetDefinition(ent, languageId) != null ||
            !_language.TryGetLanguageById(languageId, out _))
            return false;

        var newDef = new LanguageDefinition(languageId, canSpeak);
        AddLanguage(ent, newDef);
        return true;
    }

    /// <summary>
    /// Adds a <see cref="LanguageDefinition"/> to the <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void AddLanguage(Entity<LanguageComponent> ent, LanguageDefinition definition)
    {
        ent.Comp.AvailableLanguages.Add(definition);
        ent.Comp.SelectedLanguage ??= definition.Id;
        Dirty(ent);
    }

    /// <summary>
    /// Clears <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void ClearLanguages(Entity<LanguageComponent> ent)
    {
        ent.Comp.AvailableLanguages.Clear();
        ent.Comp.SelectedLanguage = null;
        Dirty(ent);
    }

    /// <summary>
    /// Removes language from <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful removed</returns>
    public bool RemoveLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        var def = GetDefinition(ent, languageId);
        if (def == null)
            return false;

        if (ent.Comp.AvailableLanguages.Remove(def))
        {
            if (ent.Comp.SelectedLanguage == languageId && !TrySetLanguage(ent, 0))
                ent.Comp.SelectedLanguage = null;

            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Does the <see cref="LanguageComponent.AvailableLanguages"/> contain this language.
    /// </summary>
    public bool ContainsLanguage(Entity<LanguageComponent> ent, string languageId, bool withCanSpeak = false)
    {
        var def = GetDefinition(ent, languageId);
        if (def == null ||
            (withCanSpeak && !def.CanSpeak))
            return false;

        return true;
    }

    public static LanguageDefinition? GetDefinition(Entity<LanguageComponent> ent, string languageId)
    {
        return ent.Comp.AvailableLanguages.Find(l => l.Id == languageId);
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by <paramref name="index"/> of <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public bool TrySetLanguage(Entity<LanguageComponent> ent, int index)
    {
        if (ent.Comp.AvailableLanguages.Count <= index)
            return false;

        ent.Comp.SelectedLanguage = ent.Comp.AvailableLanguages[index].Id;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by language id.
    /// Doesn't set language if <see cref="LanguageComponent.AvailableLanguages"/> doesn't contain this <paramref name="languageId"/>
    /// </summary>
    public bool TrySetLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        if (!CanSpeak(ent, languageId))
            return false;

        ent.Comp.SelectedLanguage = languageId;
        Dirty(ent);
        return true;
    }
    #endregion

    /// <summary>
    ///     Checks whether the entity can speak this language.
    /// </summary>
    public bool CanSpeak(EntityUid uid, string languageId)
    {
        if (!TryComp<LanguageComponent>(uid, out var comp))
            return false;

        return ContainsLanguage((uid, comp), languageId, true);
    }

    /// <summary>
    ///     Checks whether the entity understands this language.
    /// </summary>
    public bool CanUnderstand(EntityUid uid, string languageId)
    {
        if (KnowsAllLanguages(uid) ||
            languageId == UniversalLanguage)
            return true;

        if (!TryComp<LanguageComponent>(uid, out var comp))
            return false;

        return ContainsLanguage((uid, comp), languageId);
    }

    /// <summary>
    ///     Checks whether the entity knows all languages.
    /// </summary>
    public bool KnowsAllLanguages(EntityUid uid)
    {
        return HasComp<GhostComponent>(uid);
    }
}
