// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using System.Linq;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

/// <summary>
/// Well it have two downsides:
/// <list  type="number">
/// <item> We believe that minimum of y is zero. For my case its true </item>
/// <item> We can plot only with increasing X cause how <see cref="PlotPoints2D"/> works </item>
/// </list>
/// good luck if you want to change it
/// </summary>
public sealed class PlotSequencedView : Plot
{
    public Color FirstGraphicColor = Color.LightGreen;
    public Color SecondGraphicColor = Color.Red;
    public float FirstGraphicThickness = 3f;
    public float SecondGraphicThickness = 3f;
    /// <summary>
    /// Defines maximum number of points that will be stored/drawn
    /// </summary>
    public int PointStored = 128;
    /// <summary>
    /// Save thing in case we somehow have ymax = ymin and etc
    /// </summary>
    public float YMaxOffset = 0.1f;
    /// <summary>
    /// Save thing if we have null in width of X data
    /// </summary>
    public float XWidthSave = 1f;
    private void DrawFirstGraphicLine(DrawingHandleScreen handle, Vector2 from, Vector2 to)
                    => DrawLine(handle, from, to, FirstGraphicThickness, FirstGraphicColor);

    private PlotPoints2D _plotPoints;
    private readonly Font _font;
    public PlotSequencedView() : base()
    {
        _font = AxisFont;
        RectClipContent = false;
        IoCManager.InjectDependencies(this);
        _plotPoints = new PlotPoints2D(PointStored);
    }
    public void LoadPlot2DTimePoints(PlotPoints2D plotPoints, LabelContainer? label = null)
    {
        if (label == null)
            plotPoints.CopyLabels(_plotPoints);
        else
            plotPoints.CopyLabels(label);
        _plotPoints = plotPoints;
    }
    public void AddPointToPlot(Vector2 point)
    {
        _plotPoints.AddPoint(point);
    }
    public void SetLabels(string? xLabel, string? yLabel, string? title)
    {
        _plotPoints.XLabel = xLabel;
        _plotPoints.YLabel = yLabel;
        _plotPoints.Title = title;
    }
    public float GetLastAddedPointX()
    {
        if (_plotPoints.Point2Ds == null)
            return float.NaN;

        return _plotPoints.Point2Ds.Last().X;
    }
    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_plotPoints == null)
            return;
        if (_plotPoints.Point2Ds == null)
            return;
        if (!_plotPoints.TryGetDeltaBetweenMaxMinX(out var xWidth))
            return;
        if (!_plotPoints.TryGetMaxY(out var yMax))
            return;
        if (!(PixelWidth - AxisBorderPosition > 0))
            return;

        var yMaxResult = yMax.Value + YMaxOffset;
        var xWidthResult = xWidth.Value;

        var deltaXWidth = PixelWidth / _plotPoints.Point2Ds.Count;
        var yNormalizer = PixelHeight / yMaxResult;

        var point2Ds = _plotPoints.Point2Ds;
        for (var i = 1; i < point2Ds.Count; i++)
        {
            var firstPoint = InsideVector(deltaXWidth * (i - 1), point2Ds[i - 1].Y * yNormalizer);
            var secondPoint = InsideVector(deltaXWidth * i, point2Ds[i].Y * yNormalizer);

            DrawFirstGraphicLine(handle, firstPoint, secondPoint);
        }
        //Draw axis here to draw it on top of other
        DrawAxis(handle, yMaxResult, xWidthResult);
    }

    private void DrawAxis(DrawingHandleScreen handle, float maxY, float xWidth)
    {
        DrawAxis(handle, _plotPoints);

        //start with drawing axises
        foreach (var step in AxisSteps)
        {
            // X
            handle.DrawString(_font, InsideVector(PixelWidth * step, 0), $"{step * xWidth - xWidth:0.}");
            // Y
            handle.DrawString(_font, InsideVector(SerifSize, PixelHeight * step), $"{step * maxY:0.}");
        }
    }
}
