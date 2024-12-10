// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Ghost;
using Robust.Shared.Prototypes;



namespace Content.Shared.SS220.Language;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    ///     Method that checks an entity for the presence of a prototype language
    ///     or for the presence of a universal language
    /// </summary>
    public bool CheckLanguage(EntityUid ent, LanguagesPrototype? proto)
    {
        if (proto == null)
            return false;

        if (KnowsUniversalLanguage(ent))
            return true;

        return KnowsLanguages(ent, proto.ID);
    }

    public bool KnowsLanguages(EntityUid ent, string languageId)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return false;

        if (comp.CurrentLanguage == languageId)
            return true;

        return comp.LearnedLanguages.Contains(languageId);
    }

    public bool KnowsUniversalLanguage(EntityUid ent)
    {
        if (HasComp<GhostComponent>(ent))
            return true;

        if (!TryComp<LanguageComponent>(ent, out var comp))
            return true;

        if (comp != null && comp.CurrentLanguage == LanguagesPrototype.Universal)
            return true;

        if (comp != null && comp.LearnedLanguages.Contains(LanguagesPrototype.Universal))
            return true;

        return false;
    }

    /// <summary>
    ///     A method to get a prototype language from an entity.
    ///     If the entity does not have a language component, a universal language is assigned.
    /// </summary>
    public LanguagesPrototype? GetProto(EntityUid ent)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
        {
            if (_proto.TryIndex<LanguagesPrototype>(LanguagesPrototype.Universal, out var universalProto))
                return universalProto;
        }

        var languageID = GetCurrentLanguage(ent);

        if (languageID == null)
            return null;

        if (_proto.TryIndex<LanguagesPrototype>(languageID, out var proto))
            return proto;

        return null;
    }

    public string? GetCurrentLanguage(EntityUid ent)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return null;

        return comp.CurrentLanguage;
    }

    public void AddLanguage(EntityUid ent, List<string> languages)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return;

        foreach(var language in languages)
        {
            if (!_proto.TryIndex<LanguagesPrototype>(language, out var proto))
                continue;

            if (!comp.LearnedLanguages.Contains(language))
                comp.LearnedLanguages.Add(language);
        }
    }

    /// <summary>
    ///     Sets the color of the prototype language to the message 
    /// </summary>
    public string SetColor(string message, LanguagesPrototype proto)
    {
        if (proto.Color == null)
            return message;

        var color = proto.Color.Value.ToHex();
        message = $"[color={color}]{message}[/color]";
        return message;
    }
}