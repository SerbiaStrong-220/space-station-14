// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.Graphics;
using System.Numerics;
using System.Security.Policy;

namespace Content.Client.SS220.UserInterface.PlotFigure;

internal abstract class Plot : Control
{
    [Dependency] internal readonly IResourceCache ResourceCache = default!;
    public Color AxisColor = Color.White;
    public float AxisThickness = 4f;
    public List<float> AxisSteps = new() { 0.2f, 0.4f, 0.6f, 0.8f };
    public float AxisBorderPosition = 20;

    public int SerifSize = 5;
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
        var leftTop = from + perpendicularClockwise * thickness / 2;
        var leftBottom = from - perpendicularClockwise * thickness / 2;
        var rightBottom = to - perpendicularClockwise * thickness / 2;
        var rightTop = to + perpendicularClockwise * thickness / 2;
        // look to this properly cause idk how it works, but in handle it the same...
        var pointList = new List<Vector2> { leftBottom, leftTop, rightBottom, rightTop };
        DrawVertexUV2D[] pointSpan = new DrawVertexUV2D[pointList.Count];
        for (var i = 0; i < pointList.Count; i++)
        {
            pointSpan[i] = new DrawVertexUV2D(pointList[i], new Vector2(0.5f, 0.5f));
        }
        // handle.DrawLine(to, from, Color.AliceBlue);
        // handle.DrawLine(new Vector2(0f, 0f), leftTop, Color.Aqua);
        // handle.DrawLine(new Vector2(0f, 0f), rightBottom, Color.BetterViolet);

        // Angle angle = Math.Atan2(fromToVector.X, fromToVector.Y);
        // var oldHandleTransform = handle.GetTransform();
        // var boxUI = new UIBox2(leftTop, rightBottom);
        // var newHandleTransform = Matrix3Helpers.CreateTransform(oldHandleTransform.M31, oldHandleTransform.M32, Math.PI / 10);
        // handle.SetTransform(newHandleTransform);
        // handle.DrawRect(boxUI, color, true);
        // handle.SetTransform(oldHandleTransform);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, pointSpan, color);
    }
    internal Vector2 CorrectVector(float x, float y)
    {
        var newX = Math.Clamp(x, 1f, PixelWidth);
        var newY = Math.Clamp(PixelHeight - y, 1f, PixelHeight);
        return new Vector2(newX, newY);
    }
    internal void DrawAxis(DrawingHandleScreen handle, LabelContainer? mainLabels = null, LabelContainer? secondLabels = null)
    {
        // TODO think of adding AxisDots here or in childs.
        //start with drawing axises
        DrawAxisLine(handle, CorrectVector(AxisBorderPosition, AxisBorderPosition), CorrectVector(PixelWidth, AxisBorderPosition));
        DrawAxisLine(handle, CorrectVector(AxisBorderPosition, AxisBorderPosition), CorrectVector(AxisBorderPosition, PixelHeight));
        foreach (var step in AxisSteps)
        {
            // X
            DrawAxisLine(handle, CorrectVector(PixelWidth * step + AxisBorderPosition, AxisBorderPosition),
                                CorrectVector(PixelWidth * step + AxisBorderPosition, AxisBorderPosition + SerifSize));
            //handle.DrawString(AxisFont, CorrectVector(PixelWidth * step, AxisBorderPosition), $"{step * maxX:0.}");
            // Y
            DrawAxisLine(handle, CorrectVector(AxisBorderPosition, PixelHeight * step),
                                CorrectVector(AxisBorderPosition + SerifSize, PixelHeight * step));
            //handle.DrawString(AxisFont, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight * step), $"{step * maxY:0.}");
        }
        //handle.DrawString(AxisFont, CorrectVector(AxisBorderPosition + 2 * SerifSize, AxisBorderPosition), $"{-xWidth:0.}");
        // here goes labels
        AddAxisLabels(handle, mainLabels);
        AddAxisLabels(handle, secondLabels);
    }
    // TODO add logic for secondLabels like moving swapping etc etc
    private void AddAxisLabels(DrawingHandleScreen handle, LabelContainer? labels)
    {
        if (labels == null)
            return;
        if (labels.YLabel != null)
            handle.DrawString(AxisFont, CorrectVector(AxisBorderPosition + SerifSize, PixelHeight), $"{labels.YLabel}");
        if (labels.XLabel != null)
            handle.DrawString(AxisFont, CorrectVector(PixelWidth - labels.XLabel.Length * FontSize, AxisBorderPosition), $"{labels.XLabel}");
        if (labels.Label != null)
            handle.DrawString(AxisFont, CorrectVector(PixelWidth / 2 - labels.Label.Length * FontSize / 2, PixelHeight / 2), $"{labels.Label}");
    }
}

public enum GraphicSeniorityEnum
{
    First = 1, // make it more obvious
    Second
}
