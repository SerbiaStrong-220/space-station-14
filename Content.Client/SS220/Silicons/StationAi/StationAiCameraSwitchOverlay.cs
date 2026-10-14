// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Silicons.StationAi;

/// <summary>
/// Short burst of VHS static over the viewport while the AI view relinks to another camera.
/// </summary>
public sealed class StationAiCameraSwitchOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "AiCameraSwitchStatic";
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan Hold = TimeSpan.FromSeconds(0.12);

    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly ShaderInstance _shader;

    public TimeSpan Start;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public StationAiCameraSwitchOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = IoCManager.Resolve<IPrototypeManager>().Index(Shader).InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _timing.RealTime - Start < Duration;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Fully static first, then fade out as the new picture locks in.
        var elapsed = _timing.RealTime - Start;
        var alpha = elapsed < Hold ? 0.9f : 0.9f * (float) (1 - (elapsed - Hold) / (Duration - Hold));
        _shader.SetParameter("alpha", Math.Clamp(alpha, 0f, 1f));
        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
