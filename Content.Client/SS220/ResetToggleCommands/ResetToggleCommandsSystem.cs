using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.SS220.ResetToggleCommands;

public sealed partial class ResetToggleCommandsSystem : EntitySystem
{
    [Dependency] private ILightManager _light = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        _player.LocalPlayerAttached += OnPlayerAttached;
        _player.LocalPlayerDetached += OnPlayerDetached;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.LocalPlayerAttached -= OnPlayerAttached;
        _player.LocalPlayerDetached -= OnPlayerDetached;
    }

    private void OnPlayerAttached(EntityUid uid)
    {
        ResetLight();
    }

    private void OnPlayerDetached(EntityUid uid)
    {
        ResetLight();
    }

    private void ResetLight()
    {
        _light.Enabled = true;
        _light.DrawHardFov = true;
        _light.DrawLighting = true;
        _light.DrawShadows = true;
    }
}
