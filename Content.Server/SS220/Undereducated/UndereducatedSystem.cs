// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Humanoid;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.Undereducated;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Undereducated;

public sealed partial class UndereducatedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedLanguageSystem _languageSystem = default!;

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();
    private static readonly Dictionary<string, ProtoId<LanguagePrototype>> SpeciesLanguageDict = new()
    {
        // Race languages
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
        // Not race languages
        ["Binary"] = "Binary", // %bin
        ["Clownish"] = "Clownish", // %clw
        ["Tradeband"] = "Tradeband", // %trd
        ["Codespeak"] = "Codespeak", // %cod
        ["Gutter"] = "Gutter", // %gt
        ["NeoRusskiya"] = "NeoRusskiya", // %ru
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UndereducatedComponent, TransformOriginalEvent>(OnBeforeAccent);
        SubscribeLocalEvent<UndereducatedComponent, MapInitEvent>(OnMapInit);
        SubscribeNetworkEvent<UndereducatedConfigRequestEvent>(OnConfigReceived);
    }

    private void OnMapInit(Entity<UndereducatedComponent> ent, ref MapInitEvent _)
    {
        GetSpokenLanguages(ent, out var spokenLanguages);
        ent.Comp.SpokenLanguages = spokenLanguages;

        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && SpeciesLanguageDict.TryGetValue(apperance.Species, out var language)
            && spokenLanguages.Contains(language)
            && _proto.TryIndex<LanguagePrototype>(language, out var languagePrototype))
            ent.Comp.Language = languagePrototype.ID;

        else if (spokenLanguages.Contains(SpeciesLanguageDict.GetValueOrDefault("Binary", "Binary")))
            ent.Comp.Language = SpeciesLanguageDict.GetValueOrDefault("Binary", "Binary");

        else if (_languageSystem.CanSpeak(ent, _languageSystem.UniversalLanguage))
            ent.Comp.Language = _languageSystem.UniversalLanguage;

        else if (_languageSystem.CanSpeak(ent, _languageSystem.GalacticLanguage))
            ent.Comp.Language = _languageSystem.GalacticLanguage;

        Dirty(ent);
    }

    private void GetSpokenLanguages(EntityUid ent, out List<string> spokenLanguages)
    {
        spokenLanguages = [];
        var allLanguages = SpeciesLanguageDict.Values.ToList();

        foreach (var language in allLanguages)
        {
            if (_languageSystem.CanSpeak(ent, language))
            {
                spokenLanguages.Add(language);
            }
        }
    }

    private void OnConfigReceived(UndereducatedConfigRequestEvent args)
    {
        var ent = GetEntity(args.NetEntity);

        if (!TryComp<UndereducatedComponent>(ent, out var comp) || comp.Tuned)
            return;

        args.Chance = Math.Clamp(args.Chance, 0f, 1f);

        if (!comp.SpokenLanguages.Contains(args.SelectedLanguage) || !_languageSystem.CanSpeak(ent, args.SelectedLanguage))
            args.SelectedLanguage = comp.Language;

        comp.Language = args.SelectedLanguage;
        comp.ChanseToReplace = args.Chance;
        comp.Tuned = true;
        Dirty(ent, comp);
    }

    private bool TryGetLanguageTag(Entity<UndereducatedComponent> ent, [NotNullWhen(true)] out string? tag)
    {
        tag = null;
        LanguagePrototype? languagePrototype;

        if (ent.Comp.Language.Length > 0
            && _languageSystem.CanSpeak(ent, ent.Comp.Language)
            && _proto.TryIndex<LanguagePrototype>(ent.Comp.Language, out languagePrototype))
        {
            tag = languagePrototype.KeyWithPrefix;
            return true;
        }

        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && SpeciesLanguageDict.TryGetValue(apperance.Species, out var language)
            && _proto.TryIndex<LanguagePrototype>(language, out languagePrototype))
        {
            tag = languagePrototype.KeyWithPrefix;
            return true;
        }

        return false;
    }

    private void OnBeforeAccent(Entity<UndereducatedComponent> ent, ref TransformOriginalEvent args)
    {
        if (string.IsNullOrEmpty(args.Message) || !TryGetLanguageTag(ent, out var tagByRace))
            return;

        var newMessage = new StringBuilder();
        var languageMessage = _languageSystem.SanitizeMessage(ent, args.Message);
        var nodesCount = languageMessage.Nodes.Count;

        languageMessage.ChangeNodes(node =>
        {
            newMessage.Clear();
            var words = SpaceRegex().Split(node.Message);
            var wordsCount = words.Length;

            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word)
                && node.Language.KeyWithPrefix != tagByRace
                && _random.Prob(ent.Comp.ChanseToReplace))
                {
                    newMessage.Append(tagByRace).Append(' ').Append(word);
                    if (nodesCount != 1 || wordsCount != 1)
                        newMessage.Append(' ').Append(node.Language.KeyWithPrefix);
                }
                else
                {
                    if (wordsCount == words.Length)
                        newMessage.Append(node.Language.KeyWithPrefix).Append(' ');
                    newMessage.Append(word);
                }
                newMessage.Append(' ');
                wordsCount--;
            }
            node.SetMessage(newMessage.ToString().Trim());
            nodesCount--;
        });

        args.Message = languageMessage.GetMessageWithLanguageKeys();
    }
}
