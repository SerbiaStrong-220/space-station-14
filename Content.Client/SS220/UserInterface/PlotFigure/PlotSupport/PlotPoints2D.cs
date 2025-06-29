// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Linq;

namespace Content.Client.SS220.UserInterface.PlotFigure;
/// <summary>
/// This class make working with time dependent plot easier
/// It is designed to have the newest dots in the end and oldest at the start
/// </summary>
public sealed class PlotPoints2D(int maxPoints) : LabelContainer
{
    public List<Vector2>? Point2Ds => _point2Ds;
    public int MaxLength => _maxAmountOfPoints;
    private List<Vector2>? _point2Ds;
    private int _maxAmountOfPoints = maxPoints;
    /// <summary> Inits plots with existing list </summary>
    /// <exception cref="Exception">If values have more entries than maxPoints.</exception>
    public PlotPoints2D(int maxPoints, List<float> values, float xDelta, float xOffset) : this(maxPoints)
    {
        if (values.Count > maxPoints)
            throw new Exception("Tried to init PlotPoints2D with longer list than maxPoints in PlotPoints2D");
        _point2Ds = new(maxPoints);
        for (var i = 0; i < values.Count; i++)
            _point2Ds.Add(new Vector2(i * xDelta + xOffset, values[i]));
    }
    public void AddPoint(Vector2 point)
    {
        _point2Ds ??= new() { point };
        // we limit number of elements in list, so if we want to add more -> delete first one
        if (_point2Ds.Count == _maxAmountOfPoints)
            _point2Ds.RemoveAt(0);

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
