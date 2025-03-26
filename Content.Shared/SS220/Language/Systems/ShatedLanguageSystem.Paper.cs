// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem
{
    public const string PaperLanguageTagName = "language";
    public const string LanguageMsgMarkup = "languagemsg";

    private Regex? _languageMarkupRegex;

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

    protected string ParseCahceKey(string key)
    {
        var bytes = Convert.FromHexString(key);
        return Encoding.UTF8.GetString(bytes);
    }
}
