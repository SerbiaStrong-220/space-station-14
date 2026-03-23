// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Language;
using Content.Client.UserInterface.RichText;
using Robust.Client.UserInterface.RichText;

namespace Content.Client.SS220.RichText;

public static class RichTextHelper
{
    /// <summary>
    /// Array of markup tags safe for use by players
    /// </summary>
    public static readonly Type[] SafeMarkupTags =
        [
            typeof(BoldItalicTag),
            typeof(BoldTag),
            typeof(BulletTag),
            typeof(ColorTag),
            typeof(HeadingTag),
            typeof(ItalicTag),
            typeof(MonoTag),
            typeof(ScrambleTag),
            typeof(FontTag),
            typeof(LanguageMessageTag)
        ];
}
