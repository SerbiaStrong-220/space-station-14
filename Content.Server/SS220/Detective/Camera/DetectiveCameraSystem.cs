// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Popups;
using Content.Server.SurveillanceCamera;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Detective.Camera;

public sealed class DetectiveCameraSystem : EntitySystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetectiveCameraComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DetectiveCameraComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnComponentStartup(EntityUid uid, DetectiveCameraComponent component, ComponentStartup args)
    {
        if (!TryComp<SurveillanceCameraComponent>(uid, out var camera))
            return;

        _camera.SetActive(uid, false, camera);
    }

    private void OnUseInHand(EntityUid uid, DetectiveCameraComponent? component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!Resolve(uid, ref component))
            return;

        if (!TryToggle(uid, args.User, component))
            return;

        args.Handled = true;
    }

    private bool TryToggle(EntityUid uid, EntityUid user, DetectiveCameraComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!TryComp<SurveillanceCameraComponent>(uid, out var cameraComponent))
            return false;

        if (component.ActivateCameraOnEnable)
        {
            _camera.SetActive(uid, !component.Enabled, cameraComponent);
        }
        component.Enabled = !component.Enabled;
        var evt = new DetectiveCameraToggledEvent(component.Enabled);
        RaiseLocalEvent(uid, evt);

        if (component.Enabled)
        {
            _popup.PopupEntity(Loc.GetString("detective-camera-enabled"), uid, user);
            _audio.PlayEntity(component.PowerOnSound, user, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("detective-camera-disabled"), uid, user);
            _audio.PlayEntity(component.PowerOffSound, user, uid);
        }

        return true;
    }
}

public readonly record struct DetectiveCameraToggledEvent(bool IsEnabled)
{

}
