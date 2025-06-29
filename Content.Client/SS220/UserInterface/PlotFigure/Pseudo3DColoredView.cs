// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Vector3 = System.Numerics.Vector3;
using Robust.Client.Graphics;
using System.Numerics;
using System.Linq;

namespace Content.Client.SS220.UserInterface.PlotFigure;

public sealed class Pseudo3DColoredView : Plot
{
    public PlotColormap Colormap = Colormaps.GoodBad;

    public Vector2? Offsets
    {
        get => _offsets;
        set
        {
            _offsets = value;
            MakeMeshgrid();
        }
    }
    public Vector2? Sizes
    {
        get => _sizes;
        set
        {
            _sizes = value;
            MakeMeshgrid();
        }
    }
    public Vector2? Steps
    {
        get => _steps;
        set
        {
            _steps = value;
            MakeMeshgrid();
        }
    }

    private Vector2? _offsets;
    private Vector2? _sizes;
    private Vector2? _steps;

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

    public void MakeMeshgrid(List<float> x, List<float> y) => _color2DPoint = new Color2DPointView(x, y, _color2DPoint);
    public void LoadColor2DPoint(List<Vector3> vector3) => _color2DPoint?.LoadData(vector3);
    public void DeleteMovingPoint() => _movingPoint = null;

    public void SetLabels(string? xLabel, string? yLabel, string? title)
    {
        if (_color2DPoint == null)
            return;
        _color2DPoint.XLabel = xLabel;
        _color2DPoint.YLabel = yLabel;
        _color2DPoint.Title = title;
    }
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
    public void MakeMeshgrid()
    {
        if (!(Sizes.HasValue && Offsets.HasValue && Steps.HasValue))
            return;

        var paramX = (Offsets.Value.X, Sizes.Value.X, Steps.Value.X);
        var paramY = (Offsets.Value.Y, Sizes.Value.Y, Steps.Value.Y);


        if (_color2DPoint == null)
        {
            //(XMeshgrid.Value.X, XMeshgrid.Value.Y, XMeshgrid.Value.Z)
            _initCachedParams.x = paramX;
            _initCachedParams.y = paramY;
        }
        _cachedParams.x = paramX;
        _cachedParams.y = paramY;

        _color2DPoint = new Color2DPointView(paramX, paramY, _color2DPoint);
    }
    public void EvalFunctionOnMeshgrid(Func<float, float, float> func)
    {
        _cachedFunction = func;
        _color2DPoint?.EvalFunction(func);
    }
    public void LoadMovingPoint(Vector2 position, Vector2 moveDirection)
    {
        if (position.X > GetFarestPossiblePlotPoint(_cachedParams.x)
            || position.Y > GetFarestPossiblePlotPoint(_cachedParams.y)
            || position.X < GetClosetPossiblePlotPoint(_cachedParams.x)
            || position.Y < GetClosetPossiblePlotPoint(_cachedParams.y))
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
    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_color2DPoint == null)
            return;

        _minZ = _color2DPoint.MinZ;
        // to differ min from max, without that correct working of Colormap isnt possible. Number should be positive and that's all
        _maxZ = _minZ == _color2DPoint.MaxZ ? _color2DPoint.MaxZ + 0.001f : _color2DPoint.MaxZ;
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

    private float GetClosetPossiblePlotPoint((float Offset, float Size, float Step) param)
    {
        return param.Offset + param.Step;
    }
    private float GetFarestPossiblePlotPoint((float Offset, float Size, float Step) param)
    {
        return param.Offset + (param.Size - 1) * param.Step;
    }
    /// <summary> Make sure that we wont get into wrong position by changing Meshgrid </summary>
    private float MakeOffsetFromCoord(float coord, (float Min, float Size, float Step) parameters)
    {
        return Math.Clamp(coord - parameters.Size / 2f * parameters.Step, parameters.Min, float.PositiveInfinity);
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
    /// <summary>
    /// Function to draw point with internal scaling of a point size
    /// </summary>
    /// <param name="scale"> scale point a little to prevent bugs with scaling of UI in different resolutions</param>
    private void DrawPoint(DrawingHandleScreen handle, (float X, float Y) coords, (float X, float Y) size, Color color, float scale = 1.05f)
    {
        // we scale a little point to prevent it
        size.X *= scale;
        size.Y *= scale;
        _uIBox2 = UIBox2.FromDimensions(coords.X - size.X / 2, coords.Y + size.Y / 2, size.X, size.Y);
        handle.DrawRect(_uIBox2, color, true);
    }
}
