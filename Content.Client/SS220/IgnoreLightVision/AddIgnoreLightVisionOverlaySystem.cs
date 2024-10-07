// Original code github.com/CM-14 Licence MIT, All edits under © SS220, EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.IgnoreLightVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.SS220.IgnoreLightVision;

public abstract class AddIgnoreLightVisionOverlaySystem<T> : SharedAddIgnoreLightVisionOverlaySystem<T> where T : AddIgnoreLightVisionOverlayComponent
{
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<T, LocalPlayerDetachedEvent>(OnDetached);
    }

    protected abstract Type GetOverlayType();
    protected abstract Overlay GetOverlayFromConstructor(float radius, float closeRadius);

    protected override void VisionChanged(Entity<T> ent)
    {

        if (ent != _player.LocalEntity)
            return;
        // Do you know what is the worst finite state automat realization?
        // After code below you will know
        switch (ent.Comp.State)
        {
            case IgnoreLightVisionOverlayState.Off:
                Off();
                break;
            case IgnoreLightVisionOverlayState.Half:
                Off();
                Half(GetOverlayFromConstructor(ent.Comp.VisionRadius, ent.Comp.HighSensitiveVisionRadius));
                break;
            case IgnoreLightVisionOverlayState.Full:
                Full(GetOverlayFromConstructor(ent.Comp.VisionRadius, ent.Comp.HighSensitiveVisionRadius));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    protected override void VisionRemoved(Entity<T> ent)
    {
        if (ent != _player.LocalEntity)
            return;

        Off();
    }

    private void OnAttached(Entity<T> ent, ref LocalPlayerAttachedEvent args)
    {
        VisionChanged(ent);
    }
    private void OnDetached(Entity<T> ent, ref LocalPlayerDetachedEvent args)
    {
        Off();
    }

    private void Off()
    {
        _overlay.RemoveOverlay(GetOverlayType());
        _light.DrawLighting = true;
    }
    private void Half(Overlay overlay)
    {
        _overlay.AddOverlay(overlay);
        _light.DrawLighting = true;
    }
    private void Full(Overlay overlay)
    {
        _overlay.AddOverlay(overlay);
        _light.DrawLighting = false;
    }
}
