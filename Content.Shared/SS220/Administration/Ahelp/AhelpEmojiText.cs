// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Administration.Ahelp;

/// <summary>Formats only catalogued Ahelp emoji; ordinary game text remains escaped.</summary>
public static class AhelpEmojiText
{
    public const string MarkupTag = "ahelpemoji";
    private const string Truncated = "…";

    private static readonly Regex EmojiTokens = new(
        @"<:[A-Za-z0-9_]+:(?<discordId>[0-9]+)>|:(?<shortcode>[A-Za-z0-9_]+):",
        RegexOptions.CultureInvariant);

    public static string ForGame(string text, IPrototypeManager prototypes)
    {
        return Replace(FormattedMessage.EscapeText(text), prototypes,
            static emoji => $"[{MarkupTag}=\"{FormattedMessage.EscapeStringParameter(emoji.ID)}\"][/{MarkupTag}]");
    }

    /// <summary>Accounts for expanded Discord markup and never truncates a custom emoji token.</summary>
    public static string ForDiscord(string text, IPrototypeManager prototypes, int maxLength)
    {
        return TruncateDiscord(Replace(text, prototypes, static emoji => emoji.DiscordMarkup), maxLength);
    }

    public static string TruncateDiscord(string text, int maxLength, string suffix = Truncated)
    {
        if (maxLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength));

        if (text.Length <= maxLength)
            return text;

        if (maxLength < suffix.Length)
            return suffix[..maxLength];

        var end = maxLength - suffix.Length;
        foreach (Match match in EmojiTokens.Matches(text))
        {
            if (match.Index >= end)
                break;

            if (end < match.Index + match.Length)
            {
                end = match.Index;
                break;
            }
        }

        if (end > 0 && char.IsHighSurrogate(text[end - 1]))
            end--;

        return text[..end] + suffix;
    }

    private static string Replace(string text, IPrototypeManager prototypes,
        Func<AhelpEmojiPrototype, string> replacement)
    {
        return EmojiTokens.Replace(text, match =>
            TryResolveEmoji(match, prototypes, out var emoji) ? replacement(emoji) : match.Value);
    }

    private static bool TryResolveEmoji(Match match, IPrototypeManager prototypes,
        [NotNullWhen(true)] out AhelpEmojiPrototype? emoji)
    {
        if (match.Groups["shortcode"] is { Success: true } code)
            return prototypes.TryIndex(code.Value, out emoji);

        // Discord names are not unique. Resolve native Discord tokens by their snowflake ID.
        var discordId = match.Groups["discordId"].Value;
        foreach (var prototype in prototypes.EnumeratePrototypes<AhelpEmojiPrototype>())
        {
            if (prototype.DiscordId != discordId)
                continue;

            emoji = prototype;
            return true;
        }

        emoji = null;
        return false;
    }
}
