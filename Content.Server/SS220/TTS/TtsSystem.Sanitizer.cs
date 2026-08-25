using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Frozen;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.TTS;

public sealed partial class TtsSystem
{
    [GeneratedRegex(@"(?<![a-zA-Zа-яёА-ЯЁ])[a-zA-Zа-яёА-ЯЁ]+?(?![a-zA-Zа-яёА-ЯЁ])", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[^a-zA-Zа-яА-ЯёЁ0-9,\-?!. ]")]
    private static partial Regex CleanCharsRegex();

    [GeneratedRegex(@"[a-zA-Z]", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex Lat2CyrRegex();

    [GeneratedRegex(@"(?<=[1-90])(\.|,)(?=[1-90])")]
    private static partial Regex DecimalSeparatorRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    private static readonly ProtoId<LocalizedDatasetPrototype> WordReplacementKeysDatasetId = "TtsSanitizerWorldReplacementKeys";
    private static readonly ProtoId<LocalizedDatasetPrototype> WordReplacementValuesDatasetId = "TtsSanitizerWorldReplacementValues";
    private FrozenDictionary<string, string> _wordReplacement = default!;

    private static readonly ProtoId<LocalizedDatasetPrototype> TranslitReplacementKeysDatasetId = "TtsSanitizerTranslitReplacementKeys";
    private static readonly ProtoId<LocalizedDatasetPrototype> TranslitReplacementValuesDatasetId = "TtsSanitizerTranslitReplacementValues";
    private FrozenDictionary<string, string> _translitReplacement = default!;

    private void InitializeSanitizer()
    {
        BuildWordReplacementCache();
        BuildTranslitReplacementCache();
    }

    private string Sanitize(string text)
    {
        text = text.Trim();
        text = WordRegex().Replace(text, ReplaceMatchedWord);
        text = CleanCharsRegex().Replace(text, "");
        text = Lat2CyrRegex().Replace(text, ReplaceLat2Cyr);
        text = DecimalSeparatorRegex().Replace(text, " целых ");
        text = DigitsRegex().Replace(text, ReplaceWord2Num);

        text = text.Trim();

        if (char.IsLetter(text[^1]))
            text += ".";

        return text;
    }

    private void BuildWordReplacementCache()
    {
        _wordReplacement = BuildReplacementCache(WordReplacementKeysDatasetId, WordReplacementValuesDatasetId);
    }

    private void BuildTranslitReplacementCache()
    {
        _translitReplacement = BuildReplacementCache(TranslitReplacementKeysDatasetId, TranslitReplacementValuesDatasetId);
    }

    private FrozenDictionary<string, string> BuildReplacementCache(ProtoId<LocalizedDatasetPrototype> keysDatasetId, ProtoId<LocalizedDatasetPrototype> valuesDatasetId)
    {
        var keysDataset = _prototypeManager.Index(WordReplacementKeysDatasetId);
        var valuesDataset = _prototypeManager.Index(WordReplacementValuesDatasetId);

        DebugTools.Assert(keysDataset.Values.Count == valuesDataset.Values.Count);

        var dict = new Dictionary<string, string>();
        for (var i = 0; i < keysDataset.Values.Count; i++)
        {
            var key = Loc.GetString(keysDataset.Values[i]);
            var value = Loc.GetString(valuesDataset.Values[i]);

            dict.Add(key, value);
        }

        return dict.ToFrozenDictionary();
    }

    private string ReplaceLat2Cyr(Match oneChar)
    {
        if (_translitReplacement.TryGetValue(oneChar.Value.ToLower(), out var replaced))
            return replaced;

        return oneChar.Value;
    }

    private string ReplaceMatchedWord(Match word)
    {
        if (_wordReplacement.TryGetValue(word.Value.ToLower(), out var replaced))
            return replaced;

        return word.Value;
    }

    private string ReplaceWord2Num(Match word)
    {
        if (!long.TryParse(word.Value, out var number))
            return word.Value;
        return NumberConverter.NumberToText(number);
    }
}

// Source: https://codelab.ru/s/csharp/digits2phrase
public static class NumberConverter
{
    private static readonly string[] Frac20Male =
    {
        "", "один", "два", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Frac20Female =
    {
        "", "одна", "две", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Hunds =
    {
        "", "сто", "двести", "триста", "четыреста",
        "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
    };

    private static readonly string[] Tens =
    {
        "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят",
        "шестьдесят", "семьдесят", "восемьдесят", "девяносто"
    };

    public static string NumberToText(long value, bool male = true)
    {
        if (value >= (long)Math.Pow(10, 15))
            return String.Empty;

        if (value == 0)
            return "ноль";

        var str = new StringBuilder();

        if (value < 0)
        {
            str.Append("минус");
            value = -value;
        }

        value = AppendPeriod(value, 1000000000000, str, "триллион", "триллиона", "триллионов", true);
        value = AppendPeriod(value, 1000000000, str, "миллиард", "миллиарда", "миллиардов", true);
        value = AppendPeriod(value, 1000000, str, "миллион", "миллиона", "миллионов", true);
        value = AppendPeriod(value, 1000, str, "тысяча", "тысячи", "тысяч", false);

        var hundreds = (int)(value / 100);
        if (hundreds != 0)
            AppendWithSpace(str, Hunds[hundreds]);

        var less100 = (int)(value % 100);
        var frac20 = male ? Frac20Male : Frac20Female;
        if (less100 < 20)
            AppendWithSpace(str, frac20[less100]);
        else
        {
            var tens = less100 / 10;
            AppendWithSpace(str, Tens[tens]);
            var less10 = less100 % 10;
            if (less10 != 0)
                str.Append(" " + frac20[less100 % 10]);
        }

        return str.ToString();
    }

    private static void AppendWithSpace(StringBuilder stringBuilder, string str)
    {
        if (stringBuilder.Length > 0)
            stringBuilder.Append(" ");
        stringBuilder.Append(str);
    }

    private static long AppendPeriod(
        long value,
        long power,
        StringBuilder str,
        string declension1,
        string declension2,
        string declension5,
        bool male)
    {
        var thousands = (int)(value / power);
        if (thousands > 0)
        {
            AppendWithSpace(str, NumberToText(thousands, male, declension1, declension2, declension5));
            return value % power;
        }
        return value;
    }

    private static string NumberToText(
        long value,
        bool male,
        string valueDeclensionFor1,
        string valueDeclensionFor2,
        string valueDeclensionFor5)
    {
        return
            NumberToText(value, male)
            + " "
            + GetDeclension((int)(value % 10), valueDeclensionFor1, valueDeclensionFor2, valueDeclensionFor5);
    }

    private static string GetDeclension(int val, string one, string two, string five)
    {
        var t = (val % 100 > 20) ? val % 10 : val % 20;

        switch (t)
        {
            case 1:
                return one;
            case 2:
            case 3:
            case 4:
                return two;
            default:
                return five;
        }
    }
}
