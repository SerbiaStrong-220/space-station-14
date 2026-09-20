// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Shared.SS220.BodyCamera;

public sealed partial class BodyCameraPowerSystem : EntitySystem
{
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SharedSurveillanceCameraSystem _surveillanceCamera = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraPowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BodyCameraPowerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BodyCameraPowerComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<BodyCameraPowerComponent, BodyCameraActiveChangedEvent>(OnActiveChanged);
    }

    private void OnStartup(Entity<BodyCameraPowerComponent> ent, ref ComponentStartup args)
    {
        UpdateVision(ent);
    }

    private void OnPowerCellChanged(Entity<BodyCameraPowerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
            _surveillanceCamera.SetActive(ent.Owner, false);

        UpdateVision(ent);
    }

    private void OnBatteryStateChanged(Entity<BodyCameraPowerComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState == BatteryState.Empty)
            _surveillanceCamera.SetActive(ent.Owner, false);

        UpdateVision(ent);
    }

    private void OnActiveChanged(Entity<BodyCameraPowerComponent> ent, ref BodyCameraActiveChangedEvent args)
    {
        UpdateVision(ent);
    }

    private void UpdateVision(Entity<BodyCameraPowerComponent> ent)
    {
        if (!TryComp<StationAiVisionComponent>(ent, out var vision))
            return;

        var charge = new GetChargeEvent();
        RaiseLocalEvent(ent.Owner, ref charge);
        var hasCharge = charge.CurrentCharge > 0f;
        var cameraActive = !TryComp<SurveillanceCameraComponent>(ent, out var camera) || camera.Active;
        _stationAi.SetVisionEnabled((ent.Owner, vision), hasCharge && cameraActive);
    }
}
