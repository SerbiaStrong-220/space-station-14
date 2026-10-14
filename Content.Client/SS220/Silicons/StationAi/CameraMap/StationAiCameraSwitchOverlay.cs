// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Silicons.StationAi.CameraMap;

/// <summary>
///     Short burst of VHS static over the viewport while the AI view moves to another camera.
/// </summary>
public sealed class StationAiCameraSwitchOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "AiCameraSwitchStatic";
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.4);
    private static readonly TimeSpan Hold = TimeSpan.FromSeconds(0.12);
    private const float MaxAlpha = 0.9f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;
    private TimeSpan _start = TimeSpan.MinValue;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public StationAiCameraSwitchOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(Shader).InstanceUnique();
    }

    public void Play()
    {
        _start = _timing.RealTime;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _timing.RealTime - _start < Duration;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Full static first, then fade out as the new picture locks in.
        var elapsed = _timing.RealTime - _start;
        var fade = elapsed < Hold ? 1f : 1f - (float) ((elapsed - Hold) / (Duration - Hold));
        _shader.SetParameter("alpha", Math.Clamp(MaxAlpha * fade, 0f, 1f));

        var handle = args.WorldHandle;
        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
