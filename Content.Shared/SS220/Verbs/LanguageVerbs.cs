// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using System.Linq;


namespace Content.Shared.SS220.Verbs;

/// <summary>
///     I was bored and lazy to understand the UI,
///     according to this language is selected with Verb at the entity
/// </summary>
// TODO: Make the language selection in the UI instead of this crap
public sealed partial class LanguageVerbs : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LanguageComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnVerb(EntityUid ent, LanguageComponent comp, ref GetVerbsEvent<Verb> args)
    {
        if (args.User != args.Target)
            return;

        if (!args.CanAccess)
            return;

        var learnedLanguages = comp.LearnedLanguages.Select(lang => lang.ToString()).ToList();
        var verbs = CreateVerbs(ent, learnedLanguages);
        foreach (var verb in verbs)
        {
            args.Verbs.Add(verb);
        }
    }

    private List<Verb> CreateVerbs(EntityUid ent, List<string> languages)
    {
        var verbs = new List<Verb>();

        if (!TryComp<LanguageComponent>(ent, out var comp))
            return verbs;

        foreach (string language in languages)
        {
            if (language == _languageSystem.UniversalLanguage) // no verb for a universal language is created
                continue;

            verbs.Add(new Verb
            {
                Text = GetName(language),
                Message = GetDescription(language),
                Category = VerbCategory.Languages,
                Disabled = language == comp.CurrentLanguage,
                Act = () => ChangeLanguage(ent, language)
            });
        }

        return verbs;
    }

    public string GetName(string language)
    {
        if (!_proto.TryIndex<LanguagesPrototype>(language, out var proto))
            return language;

        if (proto.Name == null)
            return language;

        var name = Loc.GetString(proto.Name);
        return name;
    }

    public string? GetDescription(string language)
    {
        if (!_proto.TryIndex<LanguagesPrototype>(language, out var proto))
            return null;

        if (proto.Description == null)
            return null;

        var desc = Loc.GetString(proto.Description);
        return desc;
    }

    private void ChangeLanguage(EntityUid ent, string language)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return;

        comp.CurrentLanguage = language;
        Dirty(ent, comp);
    }
}
