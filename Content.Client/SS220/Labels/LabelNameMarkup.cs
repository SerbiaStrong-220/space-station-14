// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Labels.Components;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Labels;

public static class LabelNameMarkup
{
    public static string BuildName(LabelComponent? label, string name)
    {
        if (label is not { CurrentLabel: not null } || label.LabelColor == Color.White)
            return name;

        var labelText = label.CurrentLabel;
        var plainSuffix = Loc.GetString("comp-label-format", ("baseName", string.Empty), ("label", labelText));
        if (!name.EndsWith(plainSuffix, StringComparison.Ordinal))
            return name;

        var baseName = FormattedMessage.EscapeText(name[..^plainSuffix.Length]);
        var styledLabel = $"[bold][color={label.LabelColor.ToHex()}]{labelText}[/color][/bold]";
        var styledSuffix = Loc.GetString("comp-label-format",
            ("baseName", string.Empty),
            ("label", styledLabel));

        return $"{baseName}{styledSuffix}";
    }
}
