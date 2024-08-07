using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

internal sealed class Plot2DTimeView : Control
{
    [Dependency] private readonly IResourceCache _cache = default!;
    private Plot2DTimePoints _plotPoints;
    private const int SerifSize = 5;
    private const int FontSize = 12;
    private readonly List<float> _steps = new() { 0.2f, 0.4f, 0.6f, 0.8f };
    private readonly Font _font;
    private const int AxisBorderPosition = 20;
    public Plot2DTimeView()
    {
        RectClipContent = true;
        IoCManager.InjectDependencies(this);
        _plotPoints = new Plot2DTimePoints(128);

        _font = _cache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", FontSize);
    }
    public void LoadPlot2DTimePoints(Plot2DTimePoints plotPoints)
    {
        _plotPoints = plotPoints;
    }
    public void AddPointToPlot(Vector2 point)
    {
        _plotPoints.AddPoint(point);
    }
    public void SetXLabel(string label) => _plotPoints.XLabel = label;
    public void SetYLabel(string label) => _plotPoints.YLabel = label;

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

            handle.DrawLine(firstPoint, secondPoint, Color.Black);
        }
        //Draw axis here to draw it on top of other
        DrawAxis(handle, yMaxResult, xWidthResult);
    }
    private void DrawAxis(DrawingHandleScreen handle, float maxY, float xWidth)
    {

        //start with drawing axises
        handle.DrawLine(CorrectVector(AxisBorderPosition, AxisBorderPosition), CorrectVector(PixelWidth, AxisBorderPosition), Color.Black);
        handle.DrawLine(CorrectVector(AxisBorderPosition, AxisBorderPosition), CorrectVector(AxisBorderPosition, PixelHeight), Color.Black);
        foreach (var step in _steps)
        {
            // X
            handle.DrawLine(CorrectVector(PixelWidth * step + AxisBorderPosition, AxisBorderPosition),
                                CorrectVector(PixelWidth * step + AxisBorderPosition, AxisBorderPosition + SerifSize), Color.Black);
            handle.DrawString(_font, CorrectVector(PixelWidth * step, AxisBorderPosition), $"{step * xWidth - xWidth:0.}");
            // Y
            handle.DrawLine(CorrectVector(AxisBorderPosition, PixelHeight * step),
                                CorrectVector(AxisBorderPosition + SerifSize, PixelHeight * step), Color.Black);
            handle.DrawString(_font, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight * step), $"{step * maxY:0.}");
        }
        handle.DrawString(_font, CorrectVector(AxisBorderPosition + 2 * SerifSize, AxisBorderPosition), $"{-xWidth:0.}");
        // here goes labels
        handle.DrawString(_font, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight), $"{_plotPoints.YLabel}");
        if (_plotPoints.XLabel != null)
            handle.DrawString(_font, CorrectVector(PixelWidth - _plotPoints.XLabel.Length * FontSize, AxisBorderPosition), $"{_plotPoints.XLabel}");
    }
    private Vector2 CorrectVector(float x, float y)
    {
        var newX = Math.Clamp(x, 1f, PixelWidth);
        var newY = Math.Clamp(PixelHeight - y, 1f, PixelHeight);
        return new Vector2(newX, newY);
    }
}
