// Original code github.com/CM-14 Licence MIT, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Thermals;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SS220.Thermals;

public sealed class ThermalVisionSystem : SharedThermalVisonSystem
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnThermalAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnThermalDetached);
    }
    private void OnThermalAttached(Entity<ThermalVisionComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ThermalVisionChanged(ent);
    }

    private void OnThermalDetached(Entity<ThermalVisionComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    protected override void ThermalVisionChanged(Entity<ThermalVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        switch (ent.Comp.State)
        {
            case ThermalVisionState.Off:
                Off();
                break;
            case ThermalVisionState.Half:
                Half();
                break;
            case ThermalVisionState.Full:
                Full();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void ThermalVisionRemoved(Entity<ThermalVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay(new ThermalVisionOverlay());
        _light.DrawLighting = true;
    }

    private void Half()
    {
        _overlay.AddOverlay(new ThermalVisionOverlay());
        _light.DrawLighting = true;
    }

    private void Full()
    {
        _overlay.AddOverlay(new ThermalVisionOverlay());
        _light.DrawLighting = false;
    }
}
