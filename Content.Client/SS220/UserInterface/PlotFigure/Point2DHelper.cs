using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Linq;

namespace Content.Client.SS220.UserInterface.PlotFigure;
/// <summary>
/// This class make working with time dependent plot easier
/// It is designed to have the newest dots in the end and oldest at the start
/// </summary>
public sealed class Plot2DTimePoints(int maxPoints)
{
    public string? XLabel;
    public string? YLabel;
    public List<Vector2>? Point2Ds => _point2Ds;
    public int MaxLength => _maxAmountOfPoints;
    private List<Vector2>? _point2Ds;
    private int _maxAmountOfPoints = maxPoints;

    public void AddPoint(Vector2 point)
    {
        _point2Ds ??= new() { point };

        if (_point2Ds.Count == _maxAmountOfPoints)
        {
            _point2Ds.RemoveAt(0);
        }
        if (_point2Ds[_point2Ds.Count - 1].X > point.X)
            throw new Exception("To Plot2DTimePoints added value with lesser X then last element");
        _point2Ds.Add(point);
    }
    public bool TryGetDeltaBetweenMaxMinX([NotNullWhen(true)] out float? delta)
    {
        delta = null;
        if (_point2Ds == null)
            return false;

        var xList = _point2Ds.Select(element => element.X);
        var maxX = xList.Max();
        var minX = xList.Min();
        var deltaMaxMinX = maxX - minX;
        if (deltaMaxMinX == 0)
            return false;

        delta = deltaMaxMinX;
        return true;
    }
    public bool TryGetMaxY([NotNullWhen(true)] out float? maxY)
    {
        maxY = null;
        if (_point2Ds == null)
            return false;

        var yList = _point2Ds.Select(element => element.Y);
        maxY = yList.Max();
        return true;
    }
}
