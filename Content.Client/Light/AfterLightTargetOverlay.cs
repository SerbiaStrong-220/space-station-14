using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

/// <summary>
/// This exists just to copy <see cref="BeforeLightTargetOverlay"/> to the light render target
/// </summary>
public sealed class AfterLightTargetOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; // ss220 add night vision

    public const int ContentZIndex = LightBlurOverlay.ContentZIndex + 1;

    // ss220 add night vision start
    private static readonly ProtoId<ShaderPrototype> NightVisionShader = "NightVision";
    private readonly ShaderInstance _nightVisionShader;

    public bool NightVisionEnabled { get; set; }
    public float MinLightAfterTargetOverlay;
    // ss220 add night vision end

    public AfterLightTargetOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = ContentZIndex;

        // ss220 add night vision start
        _nightVisionShader = _proto.Index(NightVisionShader).InstanceUnique();
        // ss220 add night vision end
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        var worldHandle = args.WorldHandle;

        if (viewport.Eye == null)
            return;

        var lightOverlay = _overlay.GetOverlay<BeforeLightTargetOverlay>();
        var lightRes = lightOverlay.GetCachedForViewport(args.Viewport);
        var bounds = args.WorldBounds;

        // at 1-1 render scale it's mostly fine but at 4x4 it's way too fkn big
        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var newScale = viewport.RenderScale / (Vector2.One / lightScale);

        var localMatrix =
            viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, newScale);
        var diff = (lightRes.EnlargedLightTarget.Size - viewport.LightRenderTarget.Size);
        var halfDiff = diff / 2;

        // Pixels -> Metres -> Half distance.
        // If we're zoomed in need to enlarge the bounds further.
        args.WorldHandle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                // We essentially need to draw the cropped version onto the lightrendertarget.
                var subRegion = new UIBox2i(halfDiff.X,
                    halfDiff.Y,
                    viewport.LightRenderTarget.Size.X + halfDiff.X,
                    viewport.LightRenderTarget.Size.Y + halfDiff.Y);

                worldHandle.SetTransform(localMatrix);

                // ss220 add night vision start
                // we need to do this, cause entity needs to see other entities in darkness
                if (NightVisionEnabled)
                {
                    _nightVisionShader.SetParameter("LIGHT_TEXTURE", lightRes.EnlargedLightTarget.Texture);
                    _nightVisionShader.SetParameter("MinLight", MinLightAfterTargetOverlay);

                    worldHandle.UseShader(_nightVisionShader);
                    worldHandle.DrawTextureRectRegion(lightRes.EnlargedLightTarget.Texture, bounds, subRegion: subRegion);
                    worldHandle.UseShader(null);
                }
                else
                {
                    worldHandle.DrawTextureRectRegion(lightRes.EnlargedLightTarget.Texture, bounds, subRegion: subRegion);
                }
                // ss220 add night vision end
            }, Color.Transparent);
    }
}
