using Robust.Client.ResourceManagement;
using Content.Client.Resources;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;

namespace Content.Client.SS220.Experience.UiElements;

public sealed partial class FancyTextureButton : TextureButton
{
    [Dependency] private readonly IResourceCache _resCache = default!;

    public Color? BackgroundColor { get; set; } = Color.White;

    public string PathIconNormal
    {
        set
        {
            _pathIconNormal = value;
            ReloadClassesTexture();
        }
    }
    private string _pathIconNormal = "/Textures/Interface/Nano/inverted_triangle.svg.png";

    public string PathIconPressed
    {
        set
        {
            _pathIconPressed = value;
            ReloadClassesTexture();
        }
    }
    private string _pathIconPressed = "/Textures/Interface/Nano/triangle_right.png";

    public string PathIconHover
    {
        set
        {
            _pathIconHover = value;
            ReloadClassesTexture();
        }
    }
    private string _pathIconHover = "/Textures/Interface/Nano/triangle_right_hollow.svg.png";

    public string PathIconDisabled
    {
        set
        {
            _pathIconDisabled = value;
            ReloadClassesTexture();
        }
    }
    private string _pathIconDisabled = "/Textures/Interface/Nano/triangle_right_hollow.svg.png";

    public Texture? IconNormal;
    public Texture? IconPressed;
    public Texture? IconHover;
    public Texture? IconDisabled;

    public FancyTextureButton()
    {
        IoCManager.InjectDependencies(this);

        ReloadClassesTexture();
    }

    public void ReloadClassesTexture()
    {
        IconNormal = _resCache.GetTexture(_pathIconNormal);
        IconPressed = _resCache.GetTexture(_pathIconPressed);
        IconHover = _resCache.GetTexture(_pathIconHover);
        IconDisabled = _resCache.GetTexture(_pathIconDisabled);

        TextureNormal = IconNormal;
    }

    protected override void DrawModeChanged()
    {
        base.DrawModeChanged();

        var iconToDraw = DrawMode switch
        {
            DrawModeEnum.Normal => IconNormal,
            DrawModeEnum.Pressed => IconPressed,
            DrawModeEnum.Hover => IconHover,
            DrawModeEnum.Disabled => IconDisabled,
            _ => IconNormal
        };

        TextureNormal = iconToDraw;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var size = Math.Min(PixelSizeBox.Width, PixelSizeBox.Height);

        var offsetX = (PixelSizeBox.Width - size) / 2f;
        var offsetY = (PixelSizeBox.Height - size) / 2f;

        var drawBox = UIBox2.FromDimensions(
            PixelSizeBox.Left + offsetX,
            PixelSizeBox.Top + offsetY,
            size, size);

        if (BackgroundColor is not null)
            handle.DrawRect(drawBox, BackgroundColor.Value);

        base.Draw(handle);
    }
}
