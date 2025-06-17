// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
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

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();

    private static readonly string BorgLanguage = "Binary";
    private static readonly Dictionary<string, ProtoId<LanguagePrototype>> DefaultLanguage = new()
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

        SubscribeLocalEvent<UndereducatedComponent, TransformOriginalEvent>(OnBeforeAccent);
        SubscribeLocalEvent<UndereducatedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<UndereducatedComponent> ent, ref MapInitEvent _)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent, out var apperance)
            && DefaultLanguage.TryGetValue(apperance.Species, out var language)
            && _proto.TryIndex<LanguagePrototype>(language, out var languagePrototype))
        {
            ent.Comp.Language = languagePrototype.ID;
            return;
        }

        // Для малограмотных боргов
        if (_languageSystem.CanSpeak(ent, BorgLanguage))
        {
            ent.Comp.Language = BorgLanguage;
            return;
        }

        // Для малограмотных торгоматов
        if (_languageSystem.CanSpeak(ent, _languageSystem.UniversalLanguage))
            ent.Comp.Language = _languageSystem.UniversalLanguage;
        else if (_languageSystem.CanSpeak(ent, _languageSystem.GalacticLanguage))
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
