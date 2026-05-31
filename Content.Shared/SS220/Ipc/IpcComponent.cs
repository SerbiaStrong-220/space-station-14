// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Ipc;

/// <summary>
/// Component placed on a mob to make it a IPC.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class IpcComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    [DataField]
    public EntProtoId DrainBatteryAction = "ActionDrainBattery";

    [DataField]
    public EntProtoId ChangeFaceAction = "ActionIpcChangeFace";

    [DataField]
    public EntityUid? DrainBatteryActionEntity;

    [DataField]
    public EntityUid? ChangeFaceActionEntity;

    [DataField, AutoNetworkedField]
    public bool DrainActivated;

    /// <summary>
    /// Speed when the battery is low
    /// </summary>
    [DataField]
    public float LowChargeSpeed = 0.2f;

    /// <summary>
    /// Damage from Emp Pulse
    /// </summary>
    [DataField]
    public float DamageFromEmp = 30;

    /// <summary>
    /// Ideal temp for IPC
    /// </summary>
    [DataField]
    public float NormalTemperature = 293.2f;

    /// <summary>
    /// Delta for temp when IPC battery is overheated
    /// </summary>
    [DataField]
    public float OverDelta = 20f;

    /// <summary>
    /// Delta for temp when IPC battery is at critical state
    /// </summary>
    [DataField]
    public float CritDelta = 60.0f;

    /// <summary>
    /// Base draw rate battery
    /// </summary>
    [DataField]
    public float BaseDrawRate = 0.8f;

    /// <summary>
    /// Draw rate when delta temp +-20
    /// </summary>
    [DataField]
    public float OverDrawRate = 2.5f;

    /// <summary>
    /// Draw rate when delta temp +-60
    /// </summary>
    [DataField]
    public float CritDrawRate = 5.0f;
}

public sealed partial class ToggleDrainActionEvent : InstantActionEvent
{

}

public sealed partial class OpenIpcFaceActionEvent : InstantActionEvent
{
}
