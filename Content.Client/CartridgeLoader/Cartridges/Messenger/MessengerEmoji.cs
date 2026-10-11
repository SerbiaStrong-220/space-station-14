// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Robust.Shared.Utility;

namespace Content.Client.CartridgeLoader.Cartridges.Messenger;

/// <summary>
/// Messages carry plain-text shortcodes (":fire:"), so the line edit and logs stay readable.
/// The shortcodes are turned into symbols of the emoji font only when a message is displayed.
/// </summary>
public static class MessengerEmoji
{
    private const string EmojiFontId = "Emoji";

    // symbols from this code point are not in the main font
    private const int FirstEmojiRune = 0x2600;
    private const int ZeroWidthJoiner = 0x200D;
    private const int VariationSelector = 0xFE0F;

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
    /// Replaces shortcodes and cuts the text to the given amount of symbols (not to break an emoji in half).
    /// </summary>
    public static string Preview(string text, int maxLength)
    {
        text = ReplaceShortcodes(text);

        var cut = new StringBuilder();
        var length = 0;

        foreach (var rune in text.EnumerateRunes())
        {
            if (++length > maxLength)
                return cut.Append('…').ToString();

            cut.Append(rune.ToString());
        }

        return text;
    }

    /// <summary>
    /// Builds a message where shortcodes are replaced and all emoji symbols use the emoji font.
    /// The text is added as is, so the user can't inject markup.
    /// </summary>
    public static FormattedMessage Format(string text)
    {
        text = ReplaceShortcodes(text);

        var message = new FormattedMessage();
        var run = new StringBuilder();
        var emojiRun = false;

        foreach (var rune in text.EnumerateRunes())
        {
            var isEmoji = rune.Value >= FirstEmojiRune
                          || rune.Value == ZeroWidthJoiner
                          || rune.Value == VariationSelector;

            if (isEmoji != emojiRun)
            {
                AddRun(message, run, emojiRun);
                emojiRun = isEmoji;
            }

            run.Append(rune.ToString());
        }

        AddRun(message, run, emojiRun);

        return message;
    }

    private static string ReplaceShortcodes(string text)
    {
        foreach (var (code, symbol) in All)
        {
            text = text.Replace(code, symbol);
        }

        return text;
    }

    private static void AddRun(FormattedMessage message, StringBuilder run, bool emoji)
    {
        if (run.Length == 0)
            return;

        if (emoji)
            message.PushTag(new MarkupNode("font", new MarkupParameter(EmojiFontId), null));

        message.AddText(run.ToString());

        if (emoji)
            message.Pop();

        run.Clear();
    }
}
