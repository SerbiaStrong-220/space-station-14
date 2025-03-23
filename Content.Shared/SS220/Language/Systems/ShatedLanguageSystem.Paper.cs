// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem
{
    public const string PaperLanguageTagName = "language";
    public const string LanguageMsgMarkup = "languagemsg";

    public Dictionary<string, LanguageMessage> ScrambledMessages = new();

    private const string TagStartPattern = $@"\[{PaperLanguageTagName}[^\]]*]";
    private const string TagEndPattern = $@"\[\/{PaperLanguageTagName}\]";
    private Regex _tagStartRegex = new Regex(TagStartPattern);
    private Regex _tagEndRegex = new Regex(TagEndPattern);

    private Regex? _languageMarkupRegex;

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

    public string DecryptLanguageMarkups(EntityUid reader, string message)
    {
        var pattern = @$"\[{LanguageMsgMarkup}[^\]]+\]";
        _languageMarkupRegex ??= new Regex(pattern, RegexOptions.Multiline);
        var matches = _languageMarkupRegex.Matches(message);
        var inputLeght = message.Length;
        foreach (Match m in matches)
        {
            if (!TryParseTagArg(m.Value, LanguageMsgMarkup, out var value) ||
                !ScrambledMessages.TryGetValue(value, out var langmsg))
                continue;

            if (!CanSpeak(reader, langmsg.LanguageId))
                continue;

            var leghtDelta = message.Length - inputLeght;
            var markupIndex = m.Index + leghtDelta;
            var markupLeght = m.Length;

            var langtag = GetLanguageTagFromMessage(langmsg);
            if (langtag != null)
            {
                message = message.Remove(markupIndex, markupLeght);
                message = message.Insert(markupIndex, langtag);
            }
        }

        return message;
    }

    private string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        var scrambledMessage = language.ScrambleMethod.ScrambleMessage(message, Seed);
        if (!ScrambledMessages.ContainsKey(scrambledMessage))
        {
            var langMsg = new LanguageMessage(message, scrambledMessage, language.ID);
            AddScrambledMessage(langMsg);
        }

        return $"[{LanguageMsgMarkup}=\"{scrambledMessage}\"]";
    }

    private string? GetLanguageTagFromMessage(LanguageMessage languageMessage)
    {
        if (!_language.TryGetLanguageById(languageMessage.LanguageId, out var language))
            return null;

        return $"[{PaperLanguageTagName}={language.Key}]{languageMessage.OriginalMessage}[/{PaperLanguageTagName}]";
    }

    protected virtual void AddScrambledMessage(LanguageMessage newMsg)
    {
        ScrambledMessages.Add(newMsg.ScrambledMessage, newMsg);
    }

    private bool TryParseTagArg(string input, string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        string pattern = $@"(?<={key}="")[^""]+(?="")|(?<={key}=)[^""|\s|\]]+";
        var m = Regex.Match(input, pattern);
        if (m.Success)
        {
            value = m.Value;
            return true;
        }

        return false;
    }
}

[Serializable, NetSerializable]
public readonly record struct LanguageMessage(string OriginalMessage, string ScrambledMessage, string LanguageId)
{
    public readonly string OriginalMessage = OriginalMessage;
    public readonly string ScrambledMessage = ScrambledMessage;
    public readonly string LanguageId = LanguageId;
}
