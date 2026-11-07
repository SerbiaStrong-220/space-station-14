// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays private analysis progress and stored samples from the analyzer.
/// </summary>
public sealed class MicroFiberAnalyzerBoundUserInterface : BoundUserInterface
{
    private MicroFiberAnalyzerMenu? _window;

    public MicroFiberAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<MicroFiberAnalyzerMenu>();
        _window.Eject.OnPressed += _ => SendMessage(new MicroFiberAnalyzerEjectMessage());
        _window.PrinterButton.OnPressed += _ => Print();
        _window.Analyze.OnPressed += _ => Analyze();
        _window.SaveDescription.OnPressed += _ => SaveDescription();
        SendMessage(new MicroFiberAnalyzerRequestResultsMessage());
    }

    private void Analyze()
    {
        if (_window == null)
            return;

        if (_window.Analyze.Disabled)
            return;

        if (_window.SelectedSampleId != null)
            return;

        SendMessage(new MicroFiberAnalyzerAnalyzeMessage(_window.DescriptionEdit.Text, _window.PendingSwab));
    }

    private void SaveDescription()
    {
        if (_window == null)
            return;

        if (_window.SaveDescription.Disabled)
            return;

        var swab = _window.SelectedSampleId == null ? _window.PendingSwab : null;
        SendMessage(new MicroFiberAnalyzerSetDescriptionMessage(
            _window.SelectedSampleId, _window.DescriptionEdit.Text, swab));
    }

    private void Print()
    {
        if (_window == null)
            return;

        if (_window.PrinterButton.Disabled)
            return;

        if (_window.IsComparing)
        {
            PrintComparison(_window);
            return;
        }

        if (_window.SelectedSampleId == null)
            return;

        _window.StartPrintCooldown();
        SendMessage(new MicroFiberAnalyzerPrintMessage(_window.SelectedSampleId));
    }

    private void PrintComparison(MicroFiberAnalyzerMenu window)
    {
        if (window.ComparisonFirstSampleId == null)
            return;

        if (window.ComparisonSecondSampleId == null)
            return;

        window.StartPrintCooldown();
        SendMessage(new MicroFiberAnalyzerPrintComparisonMessage(
            window.ComparisonFirstSampleId.Value, window.ComparisonSecondSampleId.Value));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_window == null)
            return;

        if (message is not MicroFiberAnalyzerResultsMessage results)
            return;

        _window.UpdateResults(results);
    }
}
