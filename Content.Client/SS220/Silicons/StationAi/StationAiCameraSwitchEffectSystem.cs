// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Silicons.StationAi;

/// <summary>
/// Local feedback when the AI view jumps to another camera: a quiet VHS blip and a short burst of static.
/// </summary>
public sealed class StationAiCameraSwitchEffectSystem : EntitySystem
{
    private static readonly SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/Silicons/ai_camera_switch.ogg",
        AudioParams.Default.WithVolume(-6f).WithVariation(0.05f));

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;

    private StationAiCameraSwitchOverlay? _overlay;

    public override void Shutdown()
    {
        base.Shutdown();
        if (_overlay != null)
            _overlays.RemoveOverlay(_overlay);
    }

    public void Play()
    {
        _audio.PlayGlobal(Sound, Robust.Shared.Player.Filter.Local(), false);
        _overlay ??= new StationAiCameraSwitchOverlay();
        _overlay.Start = _timing.RealTime;
        // Stays registered between switches; BeforeDraw skips it once the burst is over.
        _overlays.AddOverlay(_overlay);
    }
}
