// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Paper;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem
{
    public const string PaperLanguageTagName = "language";
    public const string LanguageMsgMarkup = "languagemsg";

    private const string TagStartPattern = $@"\[{PaperLanguageTagName}[^\]]*]";
    private const string TagEndPattern = $@"\[\/{PaperLanguageTagName}\]";
    private Regex _tagStartRegex = new Regex(TagStartPattern);
    private Regex _tagEndRegex = new Regex(TagEndPattern);

    private Regex? _languageMarkupRegex;

    private void OnPaperSetContentAttempt(ref PaperSetContentAttemptEvent args)
    {
        if (args.Cancelled ||
            args.Writer is not { } writer)
            return;

        args.TransformedContent = ParseLanguageTags(writer, args.TransformedContent);
    }

    public abstract string DecryptLanguageMarkups(string message, bool checkCanSpeak = true, EntityUid? reader = null);

    public MatchCollection? FindLanguageMarkups(string message)
    {
        var pattern = @$"\[{LanguageMsgMarkup}[^\]]+\]";
        _languageMarkupRegex ??= new Regex(pattern, RegexOptions.Multiline);
        return _languageMarkupRegex.Matches(message);
    }

    protected string GenerateLanguageTag(string message, LanguagePrototype language)
    {
        return $"[{PaperLanguageTagName}={language.Key}]{message}[/{PaperLanguageTagName}]";
    }

    protected bool TryParseTagArg(string input, string key, [NotNullWhen(true)] out string? value)
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

    protected abstract string GenerateLanguageMsgMarkup(string message, LanguagePrototype language);

    protected string GenerateCacheKey(string languageId, string message)
    {
        var seed = GetSeedFromString(message);
        var key = $"{languageId}/{seed}";
        var bytes = Encoding.UTF8.GetBytes(key);
        return Convert.ToHexString(bytes);
    }

    protected string ParseCahceKey(string key)
    {
        var bytes = Convert.FromHexString(key);
        return Encoding.UTF8.GetString(bytes);
    }
}
