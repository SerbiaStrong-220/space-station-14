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

    private Regex? _languageTagRegex;
    private Regex? _languageMarkupRegex;

    public string ParseLanguageTags(EntityUid source, string message)
    {
        var pattern = @$"\[{PaperLanguageTagName}[^\]]+\]";
        _languageTagRegex ??= new Regex(pattern, RegexOptions.Multiline);
        var matches = _languageTagRegex.Matches(message);
        foreach (Match m in matches)
        {
            if (!TryParseTagArg(m.Value, PaperLanguageTagName, out var languageKey) ||
                !TryParseTagArg(m.Value, "text", out var text))
                continue;

            if (!_language.TryGetLanguageByKey(languageKey, out var language) ||
                !CanSpeak(source, language.ID))
                continue;

            message = message.Remove(m.Index, m.Length);
            var markup = GenerateLanguageMsgMarkup(text, language);
            message = message.Insert(m.Index, markup);
        }

        return message;
    }

    public string DecryptLanguageMarkups(EntityUid reader, string message)
    {
        var pattern = @$"\[{LanguageMsgMarkup}[^\]]+\]";
        _languageMarkupRegex ??= new Regex(pattern, RegexOptions.Multiline);
        var matches = _languageMarkupRegex.Matches(message);
        foreach (Match m in matches)
        {
            if (!TryParseTagArg(m.Value, LanguageMsgMarkup, out var value) ||
                !ScrambledMessages.TryGetValue(value, out var langmsg))
                continue;

            if (!CanSpeak(reader, langmsg.Language.ID))
                continue;

            message = message.Remove(m.Index, m.Length);
            var langtag = GetLanguageTagFromMessage(langmsg);
            message = message.Insert(m.Index, langtag);
        }

        return message;
    }

    private string GenerateLanguageMsgMarkup(string message, LanguagePrototype language)
    {
        var scrambledMessage = language.ScrambleMethod.ScrambleMessage(message, Seed);
        if (!ScrambledMessages.ContainsKey(scrambledMessage))
        {
            var langMsg = new LanguageMessage(message, scrambledMessage, language);
            AddScrambledMessage(langMsg);
        }

        return $"[languagemsg=\"{scrambledMessage}\"]";
    }

    private string GetLanguageTagFromMessage(LanguageMessage languageMessage)
    {
        return $"[language={languageMessage.Language.Key} text=\"{languageMessage.OriginalMessage}\"]";
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
public readonly record struct LanguageMessage(string OriginalMessage, string ScrambledMessage, LanguagePrototype Language)
{
    public readonly string OriginalMessage = OriginalMessage;
    public readonly string ScrambledMessage = ScrambledMessage;
    public readonly LanguagePrototype Language = Language;
}
