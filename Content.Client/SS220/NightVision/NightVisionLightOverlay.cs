using System.Numerics;
using Content.Client.Graphics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.NightVision;

public sealed partial class NightVisionLightOverlay : Overlay
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    private readonly NightVisionSystem _system;
    private readonly ShaderInstance _shader;
    private readonly ShaderInstance _unshaded;
    private readonly OverlayResourceCache<Resources> _resources = new();

    private static readonly ProtoId<ShaderPrototype> NightVisionShader = "NightVision";
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    public NightVisionLightOverlay(NightVisionSystem system)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
        _shader = _prototypes.Index(NightVisionShader).InstanceUnique();
        _unshaded = _prototypes.Index(UnshadedShader).Instance();
    }

    internal Texture? GetOriginalLight(IClydeViewport viewport)
    {
        return _resources.GetForViewport(viewport, _ => new Resources()).OriginalLight?.Texture;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_system.GetProfile(args.Viewport) is not { } profile)
            return;

        var viewport = args.Viewport;
        var target = viewport.LightRenderTarget;
        var res = _resources.GetForViewport(viewport, _ => new Resources());
        if (res.OriginalLight?.Size != target.Size)
        {
            res.OriginalLight?.Dispose();
            res.OriginalLight = _clyde.CreateRenderTarget(target.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                new TextureSampleParameters { Filter = true },
                name: "night-vision-original-light");
        }

        var handle = args.RenderHandle.DrawingHandleScreen;
        var rect = UIBox2.FromDimensions(Vector2.Zero, target.Size);
        handle.RenderInRenderTarget(res.OriginalLight,
            () =>
            {
                handle.SetTransform(Matrix3x2.Identity);
                handle.UseShader(_unshaded);
                handle.DrawTextureRect(target.Texture, rect);
                handle.UseShader(null);
            },
            Color.Transparent);

        _shader.SetParameter("DarknessBrightness", Math.Clamp(profile.DarknessBrightness, 0f, 1f));
        handle.RenderInRenderTarget(target,
            () =>
            {
                handle.SetTransform(Matrix3x2.Identity);
                handle.UseShader(_shader);
                handle.DrawTextureRect(res.OriginalLight.Texture, rect);
                handle.UseShader(null);
            },
            null);
    }

    protected override void DisposeBehavior()
    {
        _resources.Dispose();
        _shader.Dispose();
        base.DisposeBehavior();
    }

    private sealed class Resources : IDisposable
    {
        public IRenderTexture? OriginalLight;
        public void Dispose()
        {
            OriginalLight?.Dispose();
        }
    }
}
