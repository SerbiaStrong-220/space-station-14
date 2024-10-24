// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using System.Linq;
using System.Numerics;

namespace Content.Client.SS220.UserInterface.PlotFigure;

public sealed class MovingPoint
{
    public Color PointColor = Color.Yellow;
    public Color MoveDirectionColor = Color.LightYellow;
    public float PointSize = 4f;
    private Vector2 _pointPosition;
    /// <summary> Fake move direction </summary>
    private Vector2 _moveDirection;
    private UIBox2 _uIBox2 = new();
    public MovingPoint()
    {
        _moveDirection = new();
        _pointPosition = new();
    }
    public MovingPoint(Vector2 position, Vector2 moveDirection)
    {
        Update(position, moveDirection);
    }
    public void Update(Vector2 position, Vector2 moveDirection)
    {
        _pointPosition = position;
        _moveDirection = moveDirection;
    }
    public void DrawPoint(DrawingHandleScreen handle)
    {
        handle.DrawCircle(_pointPosition, PointSize, PointColor, true);
    }
    public void DrawMovingDirection(DrawingHandleScreen handle)
    {
        DrawArrow(handle, _pointPosition, _pointPosition + 10 * _moveDirection, 6f, MoveDirectionColor, true);
        DrawArrow(handle, _pointPosition - 10 * _moveDirection, _pointPosition, 6f, MoveDirectionColor, false);
    }

    private void DrawArrow(DrawingHandleScreen handle, Vector2 from, Vector2 to, float arrowRange, Color color, bool ArrowFront)
    {
        var perpendicularClockwise = new Vector2((to - from).Y, -(to - from).X);
        perpendicularClockwise.Normalize();
        //draw Line
        DrawTriangleStrip(handle, [ from + perpendicularClockwise * arrowRange / 3,
                                    from - perpendicularClockwise * arrowRange / 3,
                                    to + perpendicularClockwise * arrowRange / 3,
                                    to - perpendicularClockwise * arrowRange / 3,], color);

        if (ArrowFront)
            DrawArrowHeadHat(handle, from, to, arrowRange, color, perpendicularClockwise);
        else
            DrawArrowEndHat(handle, to, from, arrowRange, color, perpendicularClockwise);
    }
    private void DrawArrowHeadHat(DrawingHandleScreen handle, Vector2 from, Vector2 to, float arrowRange, Color color, Vector2 perpendicularClockwise)
    {
        DrawTriangleStrip(handle, [ to + perpendicularClockwise * arrowRange,
                                    to + (to - from) * (arrowRange - 1f) / (to - from).Length(),
                                    to,
                                    to - perpendicularClockwise * arrowRange], color);
    }
    private void DrawArrowEndHat(DrawingHandleScreen handle, Vector2 from, Vector2 to, float arrowRange, Color color, Vector2 perpendicularClockwise)
    {
        DrawTriangleStrip(handle, [ to + perpendicularClockwise * arrowRange,
                                    to - (to - from) * (arrowRange - 1f) / (to - from).Length(),
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
