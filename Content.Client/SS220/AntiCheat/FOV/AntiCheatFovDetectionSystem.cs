using Content.Shared.SS220.AntiCheat.FOV;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.SS220.AntiCheat.FOV;

public sealed class AntiCheatFovDetectionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly TimeSpan _timeForUpdate = TimeSpan.FromSeconds(5);
    private TimeSpan _lastSent = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity == null)
            return;

        var now = _gameTiming.CurTime;
        if (now - _lastSent < _timeForUpdate)
            return;

        _lastSent = now;

        if (!TryComp<EyeComponent>(_player.LocalEntity, out var eye))
            return;

        var compDrawFov = eye.DrawFov;
        var compDrawLight = eye.DrawLight;

        var eyeDrawFov = eye.Eye.DrawFov;
        var eyeDrawLight = eye.Eye.DrawLight;

        var ev = new FovEvent(compDrawFov, compDrawLight, eyeDrawFov, eyeDrawLight);
        RaiseNetworkEvent(ev);
    }
}
