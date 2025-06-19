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

    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();

    private static readonly Dictionary<string, ProtoId<LanguagePrototype>> DefaultLanguage = new()
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

        SubscribeLocalEvent<UndereducatedComponent, UndereducatedConfigRequest>(OnConfigReceived);
    }

    private void OnMapInit(Entity<UndereducatedComponent> ent, ref MapInitEvent _)
    {
        List<string> spokenLanguages = [];
        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && DefaultLanguage.TryGetValue(apperance.Species, out var language)
            && _proto.TryIndex<LanguagePrototype>(language, out var languagePrototype))
            ent.Comp.Language = languagePrototype.ID;

        else if (_languageSystem.CanSpeak(ent, DefaultLanguage.GetValueOrDefault("Binary", "Binary")))
            ent.Comp.Language = DefaultLanguage.GetValueOrDefault("Binary", "Binary");

        else if (_languageSystem.CanSpeak(ent, _languageSystem.UniversalLanguage))
            ent.Comp.Language = _languageSystem.UniversalLanguage;

        else if (_languageSystem.CanSpeak(ent, _languageSystem.GalacticLanguage))
            ent.Comp.Language = _languageSystem.GalacticLanguage;

        FillSpokenLanguage(ent, out spokenLanguages);
        ent.Comp.SpokenLanguages = spokenLanguages;
        Dirty(ent);
    }

    private void FillSpokenLanguage(Entity<UndereducatedComponent> ent, out List<string> spokenLanguages)
    {
        spokenLanguages = [];
        var allLanguages = DefaultLanguage.Values.ToList();

        foreach (var language in allLanguages)
        {
            if (_languageSystem.CanSpeak(ent, language))
            {
                spokenLanguages.Add(language);
            }
        }
    }

    private void OnConfigReceived(Entity<UndereducatedComponent> entity, ref UndereducatedConfigRequest args)
    {
        if (entity.Comp.Tuned)
            return;

        entity.Comp.Language = args.SelectedLanguage;
        entity.Comp.ChanseToReplace = args.Chance;
        entity.Comp.Tuned = true;
        Dirty(entity);
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
            && DefaultLanguage.TryGetValue(apperance.Species, out var language)
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
