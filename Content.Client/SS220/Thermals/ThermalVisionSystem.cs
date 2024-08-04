// Original code github.com/CM-14 Licence MIT, All edits under © SS220, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Thermals;
using Content.Client.SS220.Overlays;
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
        Off(ent.Comp.ThermalVisionRadius);
    }

    protected override void ThermalVisionChanged(Entity<ThermalVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;
        // Do you know what is the worst finite state automat realization?
        // After code below you will know
        switch (ent.Comp.State)
        {
            case ThermalVisionState.Off:
                Off(ent.Comp.ThermalVisionRadius);
                break;
            case ThermalVisionState.Half:
                Off(ent.Comp.ThermalVisionRadius);
                Half(ent.Comp.ThermalVisionRadius);
                break;
            case ThermalVisionState.Full:
                Full(ent.Comp.ThermalVisionRadius);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override void ThermalVisionRemoved(Entity<ThermalVisionComponent> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off(ent.Comp.ThermalVisionRadius);
    }

    private void Off(float radius)
    {
        _overlay.RemoveOverlay(new ThermalVisionOverlay(radius));
        _light.DrawLighting = true;
    }

    private void Half(float radius)
    {
        _overlay.AddOverlay(new ThermalVisionOverlay(radius));
        _light.DrawLighting = true;
    }

    private void Full(float radius)
    {
        _overlay.AddOverlay(new ThermalVisionOverlay(radius));
        _light.DrawLighting = false;
    }
}
