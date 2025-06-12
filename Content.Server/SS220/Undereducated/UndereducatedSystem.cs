// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Speech;
using Content.Shared.Humanoid;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Undereducated;

public sealed partial class UndereducatedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!;

    [GeneratedRegex(@"\b\w+\b", RegexOptions.Compiled)]
    private static partial Regex WordBoundaryRegex();

    [GeneratedRegex(@"(\w+|\W+)", RegexOptions.Compiled)]
    private static partial Regex WordTokenRegex();

    [GeneratedRegex(@"^\w+$", RegexOptions.Compiled)]
    private static partial Regex IsWordRegex();

    private Dictionary<string, ProtoId<LanguagePrototype>> _defaultLanguage = new()
    {
        ["Human"] = "SolCommon", // %sl
        ["Reptilian"] = "Sintaunathi", // %sin
        ["Tajaran"] = "Siiktajr", // %sii
        ["Vox"] = "VoxPidgin", // %vox
        ["Diona"] = "Rootspeak", // %rt
        ["SlimePerson"] = "Bubblish", // %bbl
        ["Moth"] = "Tkachi", // %tch
        ["Dwarf"] = "Eldwarf", // %el
        ["Arachnid"] = "Arati", // %ara
        ["Vulpkanin"] = "Canilunzt", // %cani
    };

public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UndereducatedComponent, BeforeAccentGetEvent>(OnBeforeAccent);
        SubscribeLocalEvent<UndereducatedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<UndereducatedComponent> ent, ref MapInitEvent _)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && _defaultLanguage.TryGetValue(apperance.Species, out var language)
            && _proto.TryIndex<LanguagePrototype>(language, out var languagePrototype))
            ent.Comp.Language = languagePrototype.ID;
        else
            ent.Comp.Language = _languageSystem.GalacticLanguage;
    }

    private bool TryGetLanguageTag(Entity<UndereducatedComponent> ent, [NotNullWhen(true)] out string? tag)
    {
        tag = null;
        LanguagePrototype? languagePrototype;

        // По компоненту
        if (ent.Comp.Language.Length > 0
            && _languageSystem.CanSpeak(ent, ent.Comp.Language)
            && _proto.TryIndex<LanguagePrototype>(ent.Comp.Language, out languagePrototype))
        {
            tag = languagePrototype.KeyWithPrefix;
            return true;
        }

        // По словарю
        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && _defaultLanguage.TryGetValue(apperance.Species, out var language)
            && _proto.TryIndex<LanguagePrototype>(language, out languagePrototype))
        {
            tag = languagePrototype.KeyWithPrefix;
            return true;
        }

        return false;
    }

    private void OnBeforeAccent(Entity<UndereducatedComponent> ent, ref BeforeAccentGetEvent args)
    {
        if (args.Message.Length <= 0 || !TryGetLanguageTag(ent, out var tagByRace))
            return;

        var wordsTotal = WordBoundaryRegex().Matches(args.Message).Count;
        var wordsReplaced = 0;

        var newMessage = new StringBuilder();
        var languageMessage = _languageSystem.SanitizeMessage(ent, args.Message);
        languageMessage.ChangeNodes(node =>
        {
            newMessage.Clear();
            var tokenMatches = WordTokenRegex().Matches(node.Message);
            var tokenCount = tokenMatches.Count;

            for (int i = 0; i < tokenCount; i++)
            {
                var word = tokenMatches[i].Value;
                var isWordRegex = IsWordRegex();

                if (!isWordRegex.IsMatch(word))
                {
                    newMessage.Append(word);
                    continue;
                }

                if (_random.Prob(ent.Comp.ChanseToReplace))
                {
                    newMessage.Append(tagByRace + " " + word);

                    if (i < tokenCount - 1)
                        newMessage.Append(" " + node.Language.KeyWithPrefix + " ");

                    wordsReplaced++;
                }
                else
                    newMessage.Append(word);
            }

            node.SetMessage(newMessage.ToString().Trim());
        });

        args.Message = languageMessage.GetMessageWithLanguageKeys();
    }
}
