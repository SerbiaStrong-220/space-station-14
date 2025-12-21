using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.UserInterface;

/// <summary>
/// A customizable UI button that uses a shader to render a rounded shape
/// with optional border and ripple effect.
/// </summary>
public sealed class ShapeButton : Button
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "ShapeShader";
    private readonly ShaderInstance _shader;

    /// <summary>
    /// Corner radius for rounded edges.
    /// </summary>
    public float Radius { get; set; } = 12f;

    /// <summary>
    /// Thickness of the border.
    /// </summary>
    public float BorderThickness { get; set; } = 2f;

    /// <summary>
    /// Maximum radius of the ripple effect.
    /// </summary>
    public float RippleRadius { get; set; } = 8f;

    /// <summary>
    /// Enables or disables the ripple animation when clicking.
    /// </summary>
    public bool RippleEnabled { get; set; } = false;

    /// <summary>
    /// Fill color of the button background.
    /// </summary>
    public Color FillColor { get; set; } = Color.White;

    /// <summary>
    /// Color of the button border.
    /// </summary>
    public Color BorderColor { get; set; } = Color.Black;

    /// <summary>
    /// Color of the ripple effect.
    /// </summary>
    public Color RippleColor { get; set; } = Color.Blue;

    private Vector2 _clickPosition;
    private float _clickTime;

    /// <summary>
    /// Duration of ripple effect animation, should match shader parameter.
    /// </summary>
    private const float MaxRippleTime = 0.5f;

    private bool _rippleActive;

    public ShapeButton()
    {
        IoCManager.InjectDependencies(this);
        _shader = _proto.Index(Shader).InstanceUnique();

        OnPressed += args =>
        {
            _clickPosition = args.Event.RelativePosition;
            _clickTime = 0f;
            _rippleActive = true;
        };
    }

    /// <summary>
    /// Updates the ripple effect over time.
    /// </summary>
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!RippleEnabled)
            return;

        if (!_rippleActive)
            return;

        _clickTime += args.DeltaSeconds;
        if (!(_clickTime > MaxRippleTime))
            return;

        _rippleActive = false;
        _clickTime = 999f;
    }

    /// <summary>
    /// Draws the button using the ShapeShader with the configured parameters.
    /// </summary>
    protected override void Draw(DrawingHandleScreen handle)
    {
        var box = PixelSizeBox;

        _shader.SetParameter("rect_size", new Vector2(SizeBox.Width, SizeBox.Height));
        _shader.SetParameter("radius", Radius);
        _shader.SetParameter("border_thickness", BorderThickness);
        _shader.SetParameter("fill_color", FillColor);
        _shader.SetParameter("border_color", BorderColor);
        _shader.SetParameter("ripple_enabled", RippleEnabled);
        _shader.SetParameter("ripple_color", RippleColor);
        _shader.SetParameter("ripple_width", RippleRadius);
        _shader.SetParameter("click_position", _clickPosition);
        _shader.SetParameter("time", _rippleActive ? _clickTime : 999f);

        Label.VAlign = Label.VAlignMode.Fill;
        handle.UseShader(_shader);
        handle.DrawRect(box, Color.White);
        handle.UseShader(null);
    }

    /// <summary>
    /// Code is the same as in shader
    /// </summary>
    /// <param name="point">Mouse Position</param>
    protected override bool HasPoint(Vector2 point)
    {
        var size = Size;
        var half = size / 2f;

        var local = point - half;

        var r = Radius;

        var q = new Vector2(MathF.Abs(local.X), MathF.Abs(local.Y)) - (half - new Vector2(BorderThickness)) + new Vector2(r);
        var dist = (Vector2.Max(q, Vector2.Zero)).Length() - r;

        return dist <= 0;
    }
}
