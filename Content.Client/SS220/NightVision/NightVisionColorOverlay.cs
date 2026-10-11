using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.NightVision;

public sealed partial class NightVisionColorOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> NightVisionShader = "NightVisionColor";

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;
    public override bool RequestScreenTexture => true;

    private readonly NightVisionSystem _system;
    private readonly NightVisionLightOverlay _lighting;
    private readonly ShaderInstance _shader;

    public NightVisionColorOverlay(NightVisionSystem system, NightVisionLightOverlay lighting)
    {
        IoCManager.InjectDependencies(this);
        _system = system;
        _lighting = lighting;
        _shader = _prototypes.Index(NightVisionShader).InstanceUnique();
        ZIndex = -1; // Before status icons and below hard FOV and blindness.
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _system.GetProfile(args.Viewport) != null &&
               _lighting.GetOriginalLight(args.Viewport) != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || _system.GetProfile(args.Viewport) is not { } profile ||
            _lighting.GetOriginalLight(args.Viewport) is not { } light)
        {
            return;
        }

        var tint = Color.FromSrgb(profile.Tint);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("LIGHT_TEXTURE", light);
        _shader.SetParameter("Tint", new Vector3(tint.R, tint.G, tint.B));
        _shader.SetParameter("Overexposure", profile.Overexposure);
        _shader.SetParameter("OverexposureStart", Math.Clamp(profile.OverexposureStart, 0f, 1f));
        _shader.SetParameter("OverexposureEnd", Math.Clamp(profile.OverexposureEnd, 0f, 1f));
        _shader.SetParameter("OverexposureStrength", Math.Clamp(profile.OverexposureStrength, 0f, 1f));
        _shader.SetParameter("Noise", Math.Clamp(profile.Noise, 0f, 1f));

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
