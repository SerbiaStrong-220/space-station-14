// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Labels.Components;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Labels;

public static class LabelNameMarkup
{
    public static string BuildName(LabelComponent? label, string name)
    {
        var escapedName = FormattedMessage.EscapeText(name);
        if (label is not { CurrentLabel: not null } || label.LabelColor == Color.White)
            return $"[bold]{escapedName}[/bold]";

        var escapedLabel = FormattedMessage.EscapeText(label.CurrentLabel);
        var plainSuffix = Loc.GetString("comp-label-format", ("baseName", string.Empty), ("label", escapedLabel));
        if (!escapedName.EndsWith(plainSuffix, StringComparison.Ordinal))
            return $"[bold]{escapedName}[/bold]";

        var baseName = escapedName[..^plainSuffix.Length];
        var coloredSuffix = Loc.GetString("comp-label-format",
            ("baseName", string.Empty),
            ("label", $"[color={label.LabelColor.ToHex()}]{escapedLabel}[/color]"));

        return $"[bold]{baseName}{coloredSuffix}[/bold]";
    }
}
