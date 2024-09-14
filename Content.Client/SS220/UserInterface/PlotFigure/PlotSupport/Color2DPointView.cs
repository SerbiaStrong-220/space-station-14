// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Actually you can use any if you want, cause we dont implement real vector logic
using Vector3 = System.Numerics.Vector3;
using System.Linq;

namespace Content.Client.SS220.UserInterface.PlotFigure;
/// <summary>
/// This class make working with colored plots made of (x,y,z) where Z is Color intensity
/// It is designed to have the newest dots in the end and oldest at the start
/// </summary>
public sealed class Color2DPointView : LabelContainer
{
    public List<float> X => _x;
    public List<float> Y => _y;
    public float MaxZ => _points3D.Select(entry => entry.Z).Max();
    public float MinZ => _points3D.Select(entry => entry.Z).Min();
    public List<Vector3> Points3D => _points3D;
    private List<Vector3> _points3D = new() { };
    private List<float> _x;
    private List<float> _y;
    public Color2DPointView(List<float> x, List<float> y, LabelContainer? labelContainer = null) : base()
    {
        _x = x;
        _y = y;

        if (labelContainer != null)
            CopyLabels(labelContainer);
    }
    public Color2DPointView((float xOffset, float xSize, float xStep) xParams,
                            (float yOffset, float ySize, float yStep) yParams,
                            LabelContainer? labelContainer = null) : base()
    {
        _x = MakeCoordFrom(xParams.xOffset, xParams.xSize, xParams.xStep);
        _y = MakeCoordFrom(yParams.yOffset, yParams.ySize, yParams.yStep);

        if (labelContainer != null)
            this.CopyLabels(labelContainer);
    }
    public void EvalFunction(Func<float, float, float> func)
    {
        foreach (var x in _x)
            foreach (var y in _y)
                _points3D.Add(new Vector3(x, y, func(x, y)));
    }
    public Vector3 Get3DPoint(int i, int j)
    {
        return _points3D[i * Y.Count + j];
    }
    public void LoadData(List<Vector3> points)
    {
        _points3D = points;
        _x = points.Select(entry => entry.X).Distinct().ToList();
        _y = points.Select(entry => entry.Y).Distinct().ToList();
    }

    private List<float> MakeCoordFrom(float offset, float size, float step)
    {
        var returnList = new List<float>();
        for (int i = 0; i < size; i++)
            returnList.Add(offset + step * i);

        return returnList;
    }
}
