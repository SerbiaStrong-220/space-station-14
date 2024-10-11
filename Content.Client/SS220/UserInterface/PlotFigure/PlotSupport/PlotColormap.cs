// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

public sealed class Colormaps
{
    public static PlotColormap SMEmitter = new PlotColormap(new() { (0f, Color.FromHex("#FFC000")), (1f, Color.FromHex("#510FAD")) });
    public static PlotColormap Jet = new PlotColormap(new() { (0f, Color.DarkBlue), (1f, Color.DarkRed) });
    public static PlotColormap Thermal = new PlotColormap(new() { (0f, Color.Black), (1f, Color.DarkRed) });
    public static PlotColormap Gray = new PlotColormap(new() { (0f, Color.Black), (1f, Color.White) });
    public static PlotColormap Autumn = new PlotColormap(new() { (0f, Color.DarkRed), (1f, Color.Yellow) });
    public static PlotColormap GoodBad = new PlotColormap(new() { (0f, Color.LightGreen), (1f, Color.DarkRed) });
}

public sealed class PlotColormap
{
    /// <summary>
    /// Here goes points between colormap interpolate Colors
    /// In Ratio you should use numbers between 0 to 1
    /// </summary>
    private List<(float Ratio, Color Value)> _keyPoints = new() { };

    public PlotColormap(List<(float Ratio, Color Value)> keyPoints)
    {
        if (keyPoints.Count < 2)
            throw new Exception("Constructed PlotColormap with less than two points");
        _keyPoints = keyPoints;
    }

    /// <summary>  </summary>
    /// <param name="ratio"> Have to be between 0 and 1 </param>
    public Color GetCorrespondingColor(float ratio)
    {
        for (var i = 1; i < _keyPoints.Count; i++)
            if (ratio <= _keyPoints[i].Ratio)
                return InterpolateBetween(_keyPoints[i - 1], _keyPoints[i], ratio);

        return Color.Black;
    }
    public void DrawColorBar(DrawingHandleScreen handle)
    {
        //Times I forgot about it: x3
    }

    private Color InterpolateBetween((float Ratio, Color Value) lesserPoint, (float Ratio, Color Value) seniorPoint, float ratio)
    {
        //LetsMakeItFUNNY
        var resultedRatio = (ratio - lesserPoint.Ratio) / (seniorPoint.Ratio - lesserPoint.Ratio);
        var returnColor = new Color(seniorPoint.Value.R * resultedRatio + lesserPoint.Value.R * (1 - resultedRatio),
                                    seniorPoint.Value.G * resultedRatio + lesserPoint.Value.G * (1 - resultedRatio),
                                    seniorPoint.Value.B * resultedRatio + lesserPoint.Value.B * (1 - resultedRatio),
                                    // Special for Stalengd
                                    seniorPoint.Value.A * resultedRatio + lesserPoint.Value.A * (1 - resultedRatio));
        return returnColor;
    }
}
