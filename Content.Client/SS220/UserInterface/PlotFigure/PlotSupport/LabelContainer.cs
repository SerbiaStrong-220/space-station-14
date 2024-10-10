// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Client.SS220.UserInterface.PlotFigure;

public abstract class LabelContainer()
{
    public string? XLabel;
    public string? YLabel;
    public string? Title;

    public LabelContainer(string xLabel, string yLabel, string title) : this()
    {
        XLabel = xLabel;
        YLabel = yLabel;
        Title = title;
    }
    public void CopyLabels(LabelContainer otherLabelContainer)
    {
        XLabel = otherLabelContainer.XLabel;
        YLabel = otherLabelContainer.YLabel;
        Title = otherLabelContainer.Title;
    }
}
