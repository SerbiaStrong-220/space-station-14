using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Guidebook.Richtext;

[UsedImplicitly]
public sealed class Table : TableContainer, IDocumentTag
{
    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        HorizontalExpand = true;
        control = this;

        if (!args.TryGetValue("Columns", out var columns) || !int.TryParse(columns, out var columnsCount))
        {
            Logger.Error("Guidebook tag \"Table\" does not specify required property \"Columns.\"");
            control = null;
            return false;
        }

        Columns = columnsCount;

        // SS220-Cult_cleaning start
        // Reserve space for short columns beside long descriptions.
        if (args.TryGetValue("MinColumnWidth", out var minWidth))
        {
            if (!float.TryParse(minWidth, NumberStyles.Float, CultureInfo.InvariantCulture, out var width) ||
                !float.IsFinite(width) || width <= 0)
            {
                Logger.Error("Guidebook Table has an invalid MinColumnWidth.");
                control = null;
                return false;
            }

            MinForcedColumnWidth = width;
        }
        // SS220-Cult_cleaning end
        return true;
    }
}
