// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

public abstract class Plot : Control
{
    [Dependency] internal readonly IResourceCache ResourceCache = default!;

    public Color AxisColor = Color.WhiteSmoke;
    public List<float> AxisSteps = new() { 0.2f, 0.4f, 0.6f, 0.8f, 1f};
    public float AxisBorderPosition = 20f;
    public float AxisThickness = 4f;
    public float SerifSize = 5f;
    public int FontSize = 12;
    public Font AxisFont;

    internal void DrawLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color)
                        => handle.DrawLine(from, to, color);
    internal void DrawLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, float thickness, Color color)
                        => DrawThickLine(handle, from, to, thickness, color);
    internal void DrawAxisLine(DrawingHandleScreen handle, Vector2 from, Vector2 to)
                    => DrawLine(handle, from, to, AxisThickness, AxisColor);

    public Plot()
    {
        RectClipContent = false;
        IoCManager.InjectDependencies(this);
        AxisFont = ResourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", FontSize);
    }
    internal void DrawThickLine(DrawingHandleScreen handle, Vector2 from, Vector2 to, float thickness, Color color)
    {
        var fromToVector = to - from;
        var perpendicularClockwise = new Vector2(fromToVector.Y, -fromToVector.X); // bruh it lefthanded
        perpendicularClockwise.Normalize();
        var leftTop = from + perpendicularClockwise * thickness / 2f;
        var leftBottom = from - perpendicularClockwise * thickness / 2f;
        var rightBottom = to - perpendicularClockwise * thickness / 2f;
        var rightTop = to + perpendicularClockwise * thickness / 2f;

        var pointList = new List<Vector2> { leftBottom, leftTop, rightBottom, rightTop };
        DrawVertexUV2D[] pointSpan = new DrawVertexUV2D[pointList.Count];
        for (var i = 0; i < pointList.Count; i++)
        {
            pointSpan[i] = new DrawVertexUV2D(pointList[i], new Vector2(0.5f, 0.5f));
        }
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, pointSpan, color);
    }

    internal void DrawAxis(DrawingHandleScreen handle, LabelContainer? mainLabels = null, LabelContainer? secondLabels = null)
    {
        //Drawing axises
        // X, first one to make axises smoothly on each other
        DrawAxisLine(handle, CorrectVector(AxisBorderPosition - AxisThickness / 2f, AxisBorderPosition), CorrectVector(PixelWidth, AxisBorderPosition));
        DrawArrowHeadHat(handle, CorrectVector(PixelWidth - SerifSize, AxisBorderPosition), CorrectVector(PixelWidth, AxisBorderPosition),
                    SerifSize, AxisColor, new Vector2(0, 1));
        // Y
        DrawAxisLine(handle, CorrectVector(AxisBorderPosition, AxisBorderPosition), CorrectVector(AxisBorderPosition, PixelHeight - SerifSize));
        DrawArrowHeadHat(handle, CorrectVector(AxisBorderPosition, PixelHeight - AxisBorderPosition), CorrectVector(AxisBorderPosition, PixelHeight - SerifSize),
                    SerifSize, AxisColor, new Vector2(1, 0));
        foreach (var step in AxisSteps)
        {
            // X
            DrawAxisLine(handle, InsideVector(PixelWidth * step, 0f),
                                InsideVector(PixelWidth * step, SerifSize));
            // Y
            DrawAxisLine(handle, InsideVector(0f, PixelHeight * step),
                                InsideVector(SerifSize, PixelHeight * step));
        }
        // adding labels
        AddAxisLabels(handle, mainLabels);
        AddAxisLabels(handle, secondLabels);
    }
    // TODO add logic for secondLabels like moving swapping etc etc
    // good luck guys =)
    private void AddAxisLabels(DrawingHandleScreen handle, LabelContainer? labels)
    {
        if (labels == null)
            return;
        if (labels.YLabel != null)
            handle.DrawString(AxisFont, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight - AxisBorderPosition), labels.YLabel, AxisColor);
        if (labels.XLabel != null)
            handle.DrawString(AxisFont, CorrectVector(PixelWidth - FontSize * 3f / 4f * labels.XLabel.Length, 2f * AxisBorderPosition + SerifSize), labels.XLabel, AxisColor);
        if (labels.Title != null)
            handle.DrawString(AxisFont, CorrectVector(PixelWidth / 2f - FontSize * labels.Title.Length / 4f, PixelHeight), labels.Title, AxisColor);
    }
    internal Vector2 CorrectVector(float x, float y)
    {
        return new Vector2(GetCorrectX(x), GetCorrectY(y));
    }

    internal Vector2 CorrectVector(Vector2 vector)
    {
        return new Vector2(GetCorrectX(vector.X), GetCorrectY(vector.Y));
    }
    internal float GetCorrectX(float x)
    {
        return Math.Clamp(x, 0f, PixelWidth);
    }
    internal float GetCorrectY(float y)
    {
        return Math.Clamp(PixelHeight - y, 0f, PixelHeight);
    }
    /// <summary> Helps with resizing into axises plot </summary>
    internal Vector2 InsideVector(float x, float y)
    {
        return new Vector2(GetInsideX(x), GetInsideY(y));
    }
    internal Vector2 InsideVector(Vector2 vector)
    {
        return new Vector2(GetInsideX(vector.X), GetInsideY(vector.Y));
    }
    internal float GetInsideX(float x)
    {
        return GetCorrectX(x) / PixelWidth * (PixelWidth - 2 * AxisBorderPosition) + AxisBorderPosition;
    }
    internal float GetInsideY(float y)
    {
        y = Math.Clamp(y, 0f, MaxHeight);
        return GetCorrectY(y / PixelHeight * (PixelHeight - 3 * AxisBorderPosition) + AxisBorderPosition);
    }
    private void DrawArrowHeadHat(DrawingHandleScreen handle, Vector2 from, Vector2 to, float arrowRange, Color color, Vector2 perpendicularClockwise)
    {
        DrawTriangleStrip(handle, [ to + perpendicularClockwise * arrowRange,
                                    to + (to - from) * (arrowRange - 1f) / (to - from).Length(),
                                    to,
                                    to - perpendicularClockwise * arrowRange], color);
    }
    private void DrawTriangleStrip(DrawingHandleScreen handle, Vector2[] vectors, Color color)
    {
        Span<DrawVertexUV2D> toSpanVector = new DrawVertexUV2D[vectors.Length];
        for (var i = 0; i < vectors.Length; i++)
        {
            toSpanVector[i] = new DrawVertexUV2D(vectors[i], new Vector2(0.5f, 0.5f));
        }
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, toSpanVector, color);
    }
}
