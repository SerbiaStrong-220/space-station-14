// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.GameTicking;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Content.Client.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly LanguageManager _language = default!;

    public Action<string>? OnNodeInfoUpdated;

    // Не содержит информации о оригинальном сообщении, а лишь то, что видит кукла
    private Dictionary<string, string> KnownPaperNodes = new();

    private List<string> _requestedNodeInfo = new();

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
        _requestedNodeInfo.Remove(ev.Key);
        if (ev.Info == string.Empty)
        {
            KnownPaperNodes.Remove(ev.Key);
            return;
        }

        KnownPaperNodes[ev.Key] = ev.Info;
        OnNodeInfoUpdated?.Invoke(ev.Key);
    }

    /// <summary>
    /// Sets the default language that the entity will speak (if it knows it)
    /// </summary>
    public void SelectLanguage(string languageId)
    {
        var ev = new ClientSelectLanguageEvent(languageId);
        RaiseNetworkEvent(ev);
    }

    #region Paper
    /// <inheritdoc/>
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

            var language = GetPrototypeFromChacheKey(key);
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

    /// <summary>
    /// Gets the language prototype from the cache key
    /// </summary>
    private LanguagePrototype? GetPrototypeFromChacheKey(string key)
    {
        if (!TryParseCahceKey(key, out var parsed))
            return null;

        var languageId = parsed.Split("/")[0];
        _language.TryGetLanguageById(languageId, out var language);
        return language;
    }

    /// <summary>
    /// Requests information from the server from a hash key
    /// </summary>
    public void RequestNodeInfo(string key)
    {
        if (_requestedNodeInfo.Contains(key))
            return;

        _requestedNodeInfo.Add(key);
        var ev = new ClientRequestPaperLanguageNodeInfo(key);
        RaiseNetworkEvent(ev);
    }

    public void FindAndRequestNodeInfoForMarkups(string message)
    {
        var markups = FindLanguageMarkups(message);
        if (markups == null)
            return;

        foreach (Match m in markups)
        {
            if (!TryParseTagArg(m.Value, LanguageMsgMarkup, out var key))
                continue;

            RequestNodeInfo(key);
        }
    }

    /// <summary>
    /// Returns value from <see cref="KnownPaperNodes"/> by key
    /// </summary>
    public bool TryGetPaperMessageFromKey(string key, [NotNullWhen(true)] out string? value, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        value = null;
        language = null;
        if (!KnownPaperNodes.TryGetValue(key, out value))
            return false;

        language = GetPrototypeFromChacheKey(key);
        return value != null && language != null;
    }

    /// <inheritdoc/>
    public override string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        var key = GenerateCacheKey(language.ID, message);
        KnownPaperNodes[key] = message;
        return $"[{LanguageMsgMarkup}=\"{key}\"]";
    }
    #endregion
}
