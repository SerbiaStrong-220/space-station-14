// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Client.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly LanguageManager _language = default!;

    // Не содержит информации о оригинальном сообщении, а лишь то, что видит кукла
    private Dictionary<string, string> KnownPaperNodes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);

        SubscribeNetworkEvent<UpdateLanguageSeedEvent>(OnUpdateLanguageSeed);
        SubscribeNetworkEvent<UpdateClientPaperLanguageNodeInfo>(OnUpdateNodeInfo);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        KnownPaperNodes.Clear();
    }

    private void OnUpdateLanguageSeed(UpdateLanguageSeedEvent ev)
    {
        Seed = ev.Seed;
    }

    private void OnUpdateNodeInfo(UpdateClientPaperLanguageNodeInfo ev)
    {
        if (ev.Info == string.Empty)
        {
            KnownPaperNodes.Remove(ev.Key);
            return;
        }

        KnownPaperNodes[ev.Key] = ev.Info;
    }

    public void SelectLanguage(string languageId)
    {
        var ev = new ClientSelectLanguageEvent(languageId);
        RaiseNetworkEvent(ev);
    }

    #region Paper
    public override string DecryptLanguageMarkups(string message, bool checkCanSpeak = true, EntityUid? reader = null)
    {
        var matches = FindLanguageMarkups(message);
        if (matches == null)
            return message;

        var inputLeght = message.Length;
        foreach (Match m in matches)
        {
            if (!TryParseTagArg(m.Value, LanguageMsgMarkup, out var key) ||
                !KnownPaperNodes.TryGetValue(key, out var knownMessage))
                continue;

            var language = GetPrototypeFromKey(key);
            if (language == null)
                continue;

            if (checkCanSpeak && (reader == null || !CanSpeak(reader.Value, language.ID)))
                continue;

            var leghtDelta = message.Length - inputLeght;
            var markupIndex = m.Index + leghtDelta;
            var markupLeght = m.Length;

            var langtag = GenerateLanguageTag(knownMessage, language);
            {
                message = message.Remove(markupIndex, markupLeght);
                message = message.Insert(markupIndex, langtag);
            }
        }

        return message;
    }

    private LanguagePrototype? GetPrototypeFromKey(string key)
    {
        key = ParseCahceKey(key);
        var languageId = key.Split("/")[0];
        _language.TryGetLanguageById(languageId, out var language);
        return language;
    }

    public void RequestNodeInfo(string key)
    {
        var ev = new ClientRequestPaperLanguageNodeInfo(key);
        RaiseNetworkEvent(ev);
    }

    public bool TryGetPaperMessageFromKey(string key, [NotNullWhen(true)] out string? value)
    {
        return KnownPaperNodes.TryGetValue(key, out value);
    }

    protected override string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        uint charSum = 0;
        foreach (var c in message.ToCharArray())
            charSum += c;

        var key = GenerateCacheKey(language.ID, message);
        KnownPaperNodes[key] = message;
        return $"[{LanguageMsgMarkup}=\"{key}\"]";
    }
#endregion
}
