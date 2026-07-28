using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Controls;

/// <summary>
/// Row of dots marking which page of something is showing; the filled dot is the active one. Shared by the
/// emote wheel and its editor so both indicate slots the same way.
/// </summary>
// SS220-emote-wheel-rework
public sealed class PageDots : Control
{
    private const float DotRadius = 4f;
    private const float DotSpacing = 16f;

    private readonly Color _activeColor = Color.White;
    private readonly Color _inactiveColor = new(255, 255, 255, 70);
    private readonly Color _outlineColor = new(0, 0, 0, 160);

    private int _count;

    /// <summary> Number of dots to draw. </summary>
    public int Count
    {
        get => _count;
        set
        {
            if (_count == value)
                return;

            _count = value;
            InvalidateMeasure();
        }
    }

    /// <summary> Index of the filled dot. </summary>
    public int Active { get; set; }

    /// <inheritdoc />
    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return new Vector2(MathF.Max(Count - 1, 0) * DotSpacing + DotRadius * 2f, DotRadius * 2f);
    }

    /// <inheritdoc />
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (Count <= 1)
            return;

        var centerY = Size.Y * 0.5f;
        var startX = (Size.X - (Count - 1) * DotSpacing) * 0.5f;

        for (var i = 0; i < Count; i++)
        {
            var center = new Vector2(startX + i * DotSpacing, centerY) * UIScale;
            var radius = DotRadius * UIScale;

            handle.DrawCircle(center, radius, i == Active ? _activeColor : _inactiveColor);
            handle.DrawCircle(center, radius, _outlineColor, false);
        }
    }
}
