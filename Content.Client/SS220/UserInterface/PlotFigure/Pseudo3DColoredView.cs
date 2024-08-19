// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Vector3 = System.Numerics.Vector3;
using Robust.Client.Graphics;
using System.Numerics;
using System.Linq;

namespace Content.Client.SS220.UserInterface.PlotFigure;

internal sealed class Pseudo3DColoredView : Plot
{
    public PlotColormap Colormap = Colormaps.GoodBad;
    private Color2DPointView? _color2DPoint;
    private MovingPoint? _movingPoint;
    private UIBox2 _uIBox2 = new();
    private Vector3 _curPoint = new();
    private (float xMax, float xMin, float yMax, float yMin) _meshgridBorders = new();
    private ((float Offset, float Size, float Step) x, (float Offset, float Size, float Step) y) _initCachedParams;
    private ((float Offset, float Size, float Step) x, (float Offset, float Size, float Step) y) _cachedParams;
    private Func<float, float, float>? _cachedFunction;
    private float _maxZ = 0f;
    private float _minZ = 0f;

    public void SetLabels(string? xLabel, string? yLabel, string? title)
    {
        if (_color2DPoint == null)
            return;
        _color2DPoint.XLabel = xLabel;
        _color2DPoint.YLabel = yLabel;
        _color2DPoint.Title = title;
    }
    public void LoadColor2DPoint(List<Vector3> vector3) => _color2DPoint?.LoadData(vector3);
    public void MakeMeshgrid((float Offset, float Size, float Step) xParams, (float Offset, float Size, float Step) yParams)
    {
        if (_color2DPoint == null)
        {
            _initCachedParams.x = xParams;
            _initCachedParams.y = yParams;
        }
        _cachedParams.x = xParams;
        _cachedParams.y = yParams;

        _color2DPoint = new Color2DPointView(xParams, yParams, _color2DPoint);
    }
    public void MakeMeshgrid(List<float> x, List<float> y) => _color2DPoint = new Color2DPointView(x, y, _color2DPoint);
    public void EvalFunctionOnMeshgrid(Func<float, float, float> func)
    {
        _cachedFunction = func;
        _color2DPoint?.EvalFunction(func);
    }
    public void LoadMovingPoint(Vector2 position, Vector2 moveDirection)
    {
        // TODO make it normal pls
        if (position.X > _cachedParams.x.Offset + (_cachedParams.x.Size - 1) * _cachedParams.x.Step
                || position.Y > _cachedParams.y.Offset + (_cachedParams.y.Size - 1) * _cachedParams.y.Step
                || position.X < _cachedParams.x.Offset || position.Y < _cachedParams.x.Offset)
        {
            MakeMeshgrid((MakeOffsetFromCoord(position.X, _initCachedParams.x), _initCachedParams.x.Size, _initCachedParams.x.Step),
                            (MakeOffsetFromCoord(position.Y, _initCachedParams.y), _initCachedParams.y.Size, _initCachedParams.y.Step));
            if (_cachedFunction != null)
                EvalFunctionOnMeshgrid(_cachedFunction);
        }

        _movingPoint ??= new MovingPoint();
        position.X = AdjustCoordToBorder(position.X, _meshgridBorders.xMin, _meshgridBorders.xMax, PixelWidth);
        position.Y = GetCorrectY(AdjustCoordToBorder(position.Y, _meshgridBorders.yMin, _meshgridBorders.yMax, PixelHeight));
        moveDirection.X = MakeToPixelRange(moveDirection.X, _meshgridBorders.xMin, _meshgridBorders.xMax, PixelWidth);
        moveDirection.Y = -1 * MakeToPixelRange(moveDirection.Y, _meshgridBorders.yMin, _meshgridBorders.yMax, PixelHeight);


        _movingPoint?.Update(position, moveDirection);
    }
    /// <summary> Make sure that we wont get into wrong position by changing Meshgrid </summary>
    private float MakeOffsetFromCoord(float coord, (float Min, float Size, float Step) parameters)
    {
        // TODO Make it to variables of plot and add difference between X and Y
        return Math.Clamp(coord - parameters.Size / 2f * parameters.Step, parameters.Min, float.PositiveInfinity);
    }

    public void DeleteMovingPoint() => _movingPoint = null;

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_color2DPoint == null)
            return;

        _maxZ = _color2DPoint.MaxZ;
        _minZ = _color2DPoint.MinZ;
        _meshgridBorders = (_color2DPoint.X.Max(), _color2DPoint.X.Min(), _color2DPoint.Y.Max(), _color2DPoint.Y.Min());

        for (var i = 0; i < _color2DPoint.X.Count; i++)
        {
            for (var j = 0; j < _color2DPoint.Y.Count; j++)
            {
                _curPoint = _color2DPoint.Get3DPoint(i, j);
                DrawPoint(handle, (AdjustCoordToBorder(_curPoint.X, _meshgridBorders.xMin, _meshgridBorders.xMax, PixelWidth),
                                GetCorrectY(AdjustCoordToBorder(_curPoint.Y, _meshgridBorders.yMin, _meshgridBorders.yMax, PixelHeight))),
                                ((PixelWidth - AxisBorderPosition) / _color2DPoint.X.Count,
                                 (PixelHeight - AxisBorderPosition) / _color2DPoint.Y.Count),
                    Colormap.GetCorrespondingColor((_curPoint.Z - _minZ) / (_maxZ - _minZ)));
            }
        }
        DrawAxis(handle, _color2DPoint);
        if (_movingPoint != null)
        {
            _movingPoint.DrawMovingDirection(handle);
            _movingPoint.DrawPoint(handle);
        }

        foreach (var step in AxisSteps)
        {
            // X
            handle.DrawString(AxisFont, InsideVector(PixelWidth * step, 0f), $"{_color2DPoint.X[(int)((_color2DPoint.X.Count - 1) * step)]:0.}");
            // Y
            handle.DrawString(AxisFont, InsideVector(SerifSize, PixelHeight * step), $"{_color2DPoint.Y[(int)((_color2DPoint.Y.Count - 1) * step)]:0.}");
        }
    }

    /// <summary> Adjust vector to borders also offsets it with AxisBorderPosition </summary>
    private float AdjustCoordToBorder(float coord, float curMin, float curMax, float availableSize)
    {
        return MakeToPixelRange(coord - curMin, curMin, curMax, availableSize) + AxisThickness / 2 + AxisBorderPosition;
    }
    private float MakeToPixelRange(float coord, float curMin, float curMax, float availableSize)
    {
        return coord / (curMax - curMin) * (availableSize - AxisBorderPosition - AxisThickness / 2);
    }
    private void DrawPoint(DrawingHandleScreen handle, (float X, float Y) coords, (float X, float Y) size, Color color)
    {
        size.X *= 1.05f;
        size.Y *= 1.05f;
        _uIBox2 = UIBox2.FromDimensions(coords.X - size.X / 2, coords.Y + size.Y / 2, size.X, size.Y);
        handle.DrawRect(_uIBox2, color, true);
    }
}
