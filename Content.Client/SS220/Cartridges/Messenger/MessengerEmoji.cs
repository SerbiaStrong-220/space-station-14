// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Cartridges.Messenger;

/// <summary>
/// Messages carry plain-text shortcodes (":fire:"), so the line edit and logs stay readable.
/// The shortcodes are turned into symbols of the emoji font only when a message is displayed.
/// </summary>
public static class MessengerEmoji
{
    private const string EmojiFontId = "Emoji";
    private const char ShortcodeMark = ':';

    public static readonly (string Code, string Symbol)[] All =
    {
        (":smile:", "\U0001F600"),
        (":grin:", "\U0001F601"),
        (":joy:", "\U0001F602"),
        (":wink:", "\U0001F609"),
        (":heart_eyes:", "\U0001F60D"),
        (":cool:", "\U0001F60E"),
        (":think:", "\U0001F914"),
        (":cry:", "\U0001F622"),
        (":angry:", "\U0001F620"),
        (":scream:", "\U0001F631"),
        (":skull:", "\U0001F480"),
        (":eyes:", "\U0001F440"),
        (":thumbsup:", "\U0001F44D"),
        (":thumbsdown:", "\U0001F44E"),
        (":clap:", "\U0001F44F"),
        (":pray:", "\U0001F64F"),
        (":ok:", "\U0001F44C"),
        (":wave:", "\U0001F44B"),
        (":heart:", "❤️"),
        (":fire:", "\U0001F525"),
        (":party:", "\U0001F389"),
        (":sos:", "\U0001F198"),
        (":warning:", "⚠️"),
        (":alarm:", "\U0001F6A8"),
        (":coffee:", "☕"),
        (":pill:", "\U0001F48A"),
        (":wrench:", "\U0001F527"),
        (":rocket:", "\U0001F680"),
    };

    /// <summary>
    /// Cuts the text to the given amount of symbols. A shortcode counts as one symbol and is never cut in half.
    /// The result still has shortcodes, pass it to <see cref="Format"/> to show.
    /// </summary>
    public static string Preview(string text, int maxLength)
    {
        var length = 0;
        var index = 0;

        while (index < text.Length)
        {
            if (++length > maxLength)
                return text[..index] + "…";

            index += SymbolLength(text, index);
        }

        return text;
    }

    /// <summary>
    /// Builds a message where known shortcodes are shown with the emoji font. The rest of the text is added as is,
    /// so the user can't inject markup.
    /// </summary>
    public static FormattedMessage Format(string text)
    {
        var message = new FormattedMessage();
        var plainStart = 0;
        var index = 0;

        while (index < text.Length)
        {
            if (!TryMatchShortcode(text, index, out var code, out var symbol))
            {
                index++;
                continue;
            }

            if (index > plainStart)
                message.AddText(text[plainStart..index]);

            AddEmoji(message, symbol);

            index += code.Length;
            plainStart = index;
        }

        if (plainStart < text.Length)
            message.AddText(text[plainStart..]);

        return message;
    }

    private static void AddEmoji(FormattedMessage message, string symbols)
    {
        message.PushTag(new MarkupNode("font", new MarkupParameter(EmojiFontId), null));
        message.AddText(symbols);
        message.Pop();
    }

    /// <summary>
    /// Length of the symbol at the index in chars: a whole shortcode, a surrogate pair or one char.
    /// </summary>
    private static int SymbolLength(string text, int index)
    {
        if (TryMatchShortcode(text, index, out var code, out _))
            return code.Length;

        return char.IsSurrogatePair(text, index) ? 2 : 1;
    }

    private static bool TryMatchShortcode(string text, int index, [NotNullWhen(true)] out string? code, [NotNullWhen(true)] out string? symbol)
    {
        code = null;
        symbol = null;

        if (text[index] != ShortcodeMark)
            return false;

        foreach (var (candidate, candidateSymbol) in All)
        {
            if (string.CompareOrdinal(text, index, candidate, 0, candidate.Length) != 0)
                continue;

            code = candidate;
            symbol = candidateSymbol;
            return true;
        }

        return false;
    }
}
