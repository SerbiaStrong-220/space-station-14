// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Player;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem
{
    private Dictionary<string, LanguageNode> PaperNodes = new();

    private void OnClientRequestPaperNodeInfo(ClientRequestPaperLanguageNodeInfo ev, EntitySessionEventArgs args)
    {
        var info = string.Empty;
        var entity = args.SenderSession.AttachedEntity;
        if (PaperNodes.TryGetValue(ev.Key, out var languageNode))
        {
            var scrambled = entity != null && !CanUnderstand(entity.Value, languageNode.Language.ID);
            info = languageNode.GetMessage(scrambled, false);
        }

        UpdateClientPaperNodeInfo(ev.Key, info, args.SenderSession);
    }

    /// <inheritdoc/>
    public override string DecryptLanguageMarkups(string message, bool checkCanSpeak = true, EntityUid? reader = null)
    {
        var matches = FindLanguageMarkups(message);
        if (matches == null)
            return message;

        var inputLeght = message.Length;
        foreach (Match m in matches)
        {
            if (!TryParseTagArg(m.Value, LanguageMsgMarkup, out var value) ||
                !PaperNodes.TryGetValue(value, out var languageNode))
                continue;

            if (checkCanSpeak && (reader == null || !CanSpeak(reader.Value, languageNode.Language.ID)))
                continue;

            var leghtDelta = message.Length - inputLeght;
            var markupIndex = m.Index + leghtDelta;
            var markupLeght = m.Length;

            var langtag = GenerateLanguageTag(languageNode.Message, languageNode.Language);
            if (langtag != null)
            {
                message = message.Remove(markupIndex, markupLeght);
                message = message.Insert(markupIndex, langtag);
            }
        }

        return message;
    }

    /// <inheritdoc/>
    public override string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        uint charSum = 0;
        foreach (var c in message.ToCharArray())
            charSum += c;

        var key = GenerateCacheKey(language.ID, message);
        if (!PaperNodes.ContainsKey(key))
        {
            var node = new LanguageNode(language, message, this);
            PaperNodes[key] = node;
        }

        return $"[{LanguageMsgMarkup}=\"{key}\"]";
    }

    private void UpdateClientPaperNodeInfo(string key, string info, ICommonSession session)
    {
        var ev = new UpdateClientPaperLanguageNodeInfo(key, info);
        RaiseNetworkEvent(ev, session);
    }
}
