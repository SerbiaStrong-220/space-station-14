// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Client.SS220.Overlays;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.SS220.TextureFade;

/// <summary>
/// Performs alpha clip on the privided texture with variable threshold (threshold filter). See <see cref="TextureFadeOverlayComponent"/> for more automatic use.
/// </summary>
public sealed class TextureFadeOverlay : StackableOverlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly SpriteSystem _spriteSystem = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public SpriteSpecifier? Sprite;
    public Color Modulate = Color.White;
    public float FadeProgress = 0f;
    public TimeSpan Time;
    public bool Loop = true;

    private readonly ShaderInstance _shader;

    public TextureFadeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _shader = _prototypeManager.Index<ShaderPrototype>("TextureFade").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        Time += TimeSpan.FromSeconds(args.DeltaSeconds);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Sprite == null)
            return;

        var texture = _spriteSystem.GetFrame(Sprite, Time, Loop);

        var worldHandle = args.WorldHandle;
        _shader.SetParameter("FadeProgress", FadeProgress);

        var viewQuad = args.WorldBounds;
        var viewSize = viewQuad.Box.Size;
        var viewRatio = viewSize.X / viewSize.Y;
        var regionSize = texture.Size;
        var regionRatio = (float)regionSize.X / regionSize.Y;
        var scaleBy = Vector2.One;
        if (viewRatio > regionRatio)
        {
            scaleBy.Y = viewRatio / regionRatio;
        }
        else
        {
            scaleBy.X = regionRatio / viewRatio;
        }
        viewQuad.Box = viewQuad.Box.Scale(scaleBy);

        worldHandle.Modulate = Modulate;
        worldHandle.UseShader(_shader);
        worldHandle.DrawTextureRectRegion(texture, viewQuad);
        worldHandle.UseShader(null);
        worldHandle.Modulate = Color.White;
    }
}
