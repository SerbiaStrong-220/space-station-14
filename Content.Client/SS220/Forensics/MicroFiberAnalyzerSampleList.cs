// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays numbered trace cards and optional empty-category messages for analyzer results and comparisons.
/// </summary>
public sealed class MicroFiberAnalyzerSampleList : BoxContainer
{
    /// <summary>
    /// Creates a vertical list whose spacing is configured by its containing view.
    /// </summary>
    public MicroFiberAnalyzerSampleList()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
    }

    /// <summary>
    /// Replaces the list with glove samples, or the supplied localized message when the list is empty.
    /// </summary>
    public void ShowGloveSamples(List<GloveMicroFiberTrace> samples, string? emptyMessage = null)
    {
        RemoveAllChildren();
        for (var i = 0; i < samples.Count; i++)
        {
            AddChild(new MicroFiberAnalyzerSampleCard(samples[i], i + 1));
        }

        if (samples.Count > 0)
            return;

        if (emptyMessage == null)
            return;

        ShowEmptyMessage(emptyMessage);
    }

    /// <summary>
    /// Replaces the list with item samples, or the supplied localized message when the list is empty.
    /// </summary>
    public void ShowStorageSamples(List<StorageMicroFiberTrace> samples, string emptyMessage)
    {
        RemoveAllChildren();
        for (var i = 0; i < samples.Count; i++)
        {
            AddChild(new MicroFiberAnalyzerSampleCard(samples[i], i + 1));
        }

        if (samples.Count > 0)
            return;

        ShowEmptyMessage(emptyMessage, MicroFiberAnalyzerColors.MutedText);
    }

    private void ShowEmptyMessage(string message, Color? color = null)
    {
        var empty = new RichTextLabel { HorizontalExpand = true };
        empty.SetMessage(FormattedMessage.FromUnformatted(message), color);
        AddChild(empty);
    }
}
