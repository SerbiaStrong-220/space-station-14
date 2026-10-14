// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.SS220.BodyCamera;
using Content.Shared.SS220.TapeRecorder;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;

namespace Content.Server.SS220.BodyCamera;

public sealed partial class BodyCameraPowerSystem : EntitySystem
{
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SharedSurveillanceCameraSystem _surveillanceCamera = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedTapeRecorderSystem _recorder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraPowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BodyCameraPowerComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<BodyCameraPowerComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        SubscribeLocalEvent<BodyCameraPowerComponent, BodyCameraActiveChangedEvent>(OnActiveChanged);
        SubscribeLocalEvent<BodyCameraPowerComponent, PowerCellSlotEmptyEvent>(OnPowerCellEmpty);
        SubscribeLocalEvent<BodyCameraPowerComponent, TapeRecorderModeChangeAttemptEvent>(OnModeChangeAttempt);
        SubscribeLocalEvent<BodyCameraPowerComponent, TapeRecorderModeChangedEvent>(OnModeChanged);
    }

    private void OnStartup(Entity<BodyCameraPowerComponent> ent, ref ComponentStartup args)
    {
        UpdatePowerAndVision(ent);
    }

    private void OnPowerCellChanged(Entity<BodyCameraPowerComponent> ent, ref PowerCellChangedEvent args)
    {
        if (args.Ejected)
            _surveillanceCamera.SetActive(ent.Owner, false);

        UpdatePowerAndVision(ent);
    }

    private void OnBatteryStateChanged(Entity<BodyCameraPowerComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (args.NewState == BatteryState.Empty)
            _surveillanceCamera.SetActive(ent.Owner, false);

        UpdatePowerAndVision(ent);
    }

    private void OnActiveChanged(Entity<BodyCameraPowerComponent> ent, ref BodyCameraActiveChangedEvent args)
    {
        UpdatePowerAndVision(ent);
    }

    private void OnPowerCellEmpty(Entity<BodyCameraPowerComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        if (TryComp<TapeRecorderComponent>(ent, out var recorder))
            _recorder.SetMode((ent.Owner, recorder), TapeRecorderMode.Stopped);

        _surveillanceCamera.SetActive(ent.Owner, false);
        UpdatePowerAndVision(ent);
    }

    private void OnModeChangeAttempt(Entity<BodyCameraPowerComponent> ent, ref TapeRecorderModeChangeAttemptEvent args)
    {
        if (!_powerCell.HasDrawCharge(ent.Owner))
            args.Cancelled = true;
    }

    private void OnModeChanged(Entity<BodyCameraPowerComponent> ent, ref TapeRecorderModeChangedEvent args)
    {
        UpdatePowerAndVision(ent);
    }

    private void UpdatePowerAndVision(Entity<BodyCameraPowerComponent> ent)
    {
        var cameraActive = TryComp<SurveillanceCameraComponent>(ent, out var camera) && camera.Active;
        var recording = TryComp<TapeRecorderComponent>(ent, out var recorder) && recorder.Mode == TapeRecorderMode.Recording;
        var drawEnabled = cameraActive || recording;
        if (TryComp<PowerCellDrawComponent>(ent, out var draw) && draw.Enabled != drawEnabled)
            _powerCell.SetDrawEnabled((ent.Owner, draw), drawEnabled);

        if (!TryComp<StationAiVisionComponent>(ent, out var vision))
            return;

        var charge = new GetChargeEvent();
        RaiseLocalEvent(ent.Owner, ref charge);
        var hasCharge = charge.CurrentCharge > 0f;
        _stationAi.SetVisionEnabled((ent.Owner, vision), hasCharge && cameraActive);
    }
}
