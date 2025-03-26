// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Paper;
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Player;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem
{
    private Dictionary<string, LanguageNode> PaperNodes = new();
    private uint _paperNodeNumber = 0;

    private const string TagStartPattern = $@"\[{PaperLanguageTagName}[^\]]*]";
    private const string TagEndPattern = $@"\[\/{PaperLanguageTagName}\]";
    private Regex _tagStartRegex = new Regex(TagStartPattern);
    private Regex _tagEndRegex = new Regex(TagEndPattern);

    private void OnPaperSetContentAttempt(ref PaperSetContentAttemptEvent args)
    {
        if (args.Cancelled ||
            args.Writer is not { } writer)
            return;

        args.TransformedContent = ParseLanguageTags(writer, args.TransformedContent);
    }

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

    public string ParseLanguageTags(EntityUid source, string message)
    {
        var tagStartMatches = _tagStartRegex.Matches(message);
        var tagEndMatches = _tagEndRegex.Matches(message);
        var inputLeght = message.Length;
        foreach (Match tagStartMatch in tagStartMatches)
        {
            foreach (Match tagEndMatch in tagEndMatches)
            {
                if (tagStartMatch.Index >= tagEndMatch.Index)
                    continue;

                var tagValue = tagStartMatch.Value;
                if (!TryParseTagArg(tagValue, PaperLanguageTagName, out var languageKey) ||
                    !_language.TryGetLanguageByKey(languageKey, out var language) ||
                    !CanSpeak(source, language.ID))
                    break;

                var leghtDelta = message.Length - inputLeght;
                var tagStartIndex = tagStartMatch.Index + leghtDelta;
                var tagEndIndex = tagEndMatch.Index + leghtDelta;

                var textIndex = tagStartIndex + tagStartMatch.Length;
                var textLeght = tagEndIndex - textIndex;
                var text = message.Substring(textIndex, textLeght);

                var nodeLeght = tagEndIndex + tagEndMatch.Length - tagStartIndex;
                message = message.Remove(tagStartIndex, nodeLeght);
                var markup = GenerateLanguageMsgMarkup(text, language);
                message = message.Insert(tagStartIndex, markup);
                break;
            }
        }

        return message;
    }

    private string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        var key = GenerateCacheKey(language.ID, message.Length);
        var node = new LanguageNode(language, message, this);
        PaperNodes.Add(key, node);
        return $"[{LanguageMsgMarkup}={key}]";
    }

    private string GenerateCacheKey(string languageId, int messageLength)
    {
        var key = $"{languageId}/{messageLength}/{_paperNodeNumber}";
        var bytes = Encoding.UTF8.GetBytes(key);
        return Convert.ToHexString(bytes);
    }

    private void UpdateClientPaperNodeInfo(string key, string info, ICommonSession session)
    {
        var ev = new UpdateClientPaperLanguageNodeInfo(key, info);
        RaiseNetworkEvent(ev, session);
    }
}
