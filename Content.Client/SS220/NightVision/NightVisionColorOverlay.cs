using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.NightVision;

public sealed class NightVisionColorOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "NightVisionColor";

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;
    private readonly ShaderInstance _nightVisionShader;

    public float MinLight;
    public float BrightThreshold;
    public float BrightBoost;
    public float Gamma;
    public float NoiseAmount;

    public NightVisionColorOverlay()
    {
        IoCManager.InjectDependencies(this);
        _nightVisionShader = _prototypeManager.Index(Shader).InstanceUnique();
        ZIndex = -9999;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.ScreenHandle;

        _nightVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _nightVisionShader.SetParameter(nameof(MinLight), MinLight);
        _nightVisionShader.SetParameter(nameof(BrightThreshold), BrightThreshold);
        _nightVisionShader.SetParameter(nameof(BrightBoost), BrightBoost);
        _nightVisionShader.SetParameter(nameof(Gamma), Gamma);
        _nightVisionShader.SetParameter(nameof(NoiseAmount), NoiseAmount);

        handle.UseShader(_nightVisionShader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);
    }
}
