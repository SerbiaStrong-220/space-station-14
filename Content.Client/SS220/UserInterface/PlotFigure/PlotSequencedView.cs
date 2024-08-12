// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

internal sealed class PlotSequencedView : Plot
{
    public Color FirstGraphicColor = Color.LightGreen;
    public Color SecondGraphicColor = Color.Red;
    public float FirstGraphicThickness = 3f;
    public float SecondGraphicThickness = 3f;
    private void DrawFirstGraphicLine(DrawingHandleScreen handle, Vector2 from, Vector2 to)
                    => DrawLine(handle, from, to, FirstGraphicThickness, FirstGraphicColor);

    private PlotPoints2D _plotPoints;
    private const int SerifSize = 12;
    private const int FontSize = 12;
    private readonly Font _font;
    public PlotSequencedView() : base()
    {
        _font = AxisFont;
        RectClipContent = true;
        IoCManager.InjectDependencies(this);
        _plotPoints = new PlotPoints2D(128);

    }
    public void LoadPlot2DTimePoints(PlotPoints2D plotPoints)
    {
        _plotPoints = plotPoints;
    }
    public void AddPointToPlot(Vector2 point)
    {
        _plotPoints.AddPoint(point);
    }
    public void SetXLabel(string label) => _plotPoints.XLabel = label;
    public void SetYLabel(string label) => _plotPoints.YLabel = label;
    public void SetLabel(string label) => _plotPoints.Label = label;

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

        var yMaxResult = (yMax + 0.1f) * 1.1f ?? 0.1f;
        var xWidthResult = xWidth ?? 1f;
        var deltaXWidth = (PixelWidth - (float) AxisBorderPosition) / _plotPoints.Point2Ds.Count;
        var yNormalizer = PixelHeight / yMaxResult;

        var point2Ds = _plotPoints.Point2Ds;
        for (var i = 1; i < point2Ds.Count; i++)
        {
            var firstPoint = CorrectVector(deltaXWidth * (i - 1) + AxisBorderPosition, point2Ds[i - 1].Y * yNormalizer);
            var secondPoint = CorrectVector(deltaXWidth * i + AxisBorderPosition, point2Ds[i].Y * yNormalizer);

            DrawFirstGraphicLine(handle, firstPoint, secondPoint);
        }
        //Draw axis here to draw it on top of other
        DrawAxis(handle, yMaxResult, xWidthResult);
    }
    private void DrawAxis(DrawingHandleScreen handle, float maxY, float xWidth)
    {
        base.DrawAxis(handle, _plotPoints);

        //start with drawing axises
        foreach (var step in AxisSteps)
        {
            // X
            handle.DrawString(_font, CorrectVector(PixelWidth * step, AxisBorderPosition), $"{step * xWidth - xWidth:0.}");
            // Y
            handle.DrawString(_font, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight * step), $"{step * maxY:0.}");
        }
        handle.DrawString(_font, CorrectVector(AxisBorderPosition + 2 * SerifSize, AxisBorderPosition), $"{-xWidth:0.}");
    }
}
