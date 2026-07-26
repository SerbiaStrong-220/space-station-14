// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Original code taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Ipc;

/// <summary>
/// Component placed on a mob to make it a IPC.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class IpcComponent : Component
{
    /// <summary>
    /// Sound played when a IPC at the critical state.
    /// </summary>
    [DataField]
    public SoundSpecifier CritStateSound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");

    /// <summary>
    /// The battery charge alert.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    /// <summary>
    /// The alert for a missing battery.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    /// <summary>
    /// Action for drain charge and charging the internal battery of the IPC (like Ninja).
    /// </summary>
    [DataField]
    public EntProtoId DrainBatteryAction = "ActionDrainBattery";

    /// <summary>
    /// Action for change IPC screen image. Based on Magic Mirror.
    /// </summary>
    [DataField]
    public EntProtoId ChangeFaceAction = "ActionIpcChangeFace";

    [DataField]
    public EntityUid? DrainBatteryActionEntity;

    [DataField]
    public EntityUid? ChangeFaceActionEntity;

    /// <summary>
    /// Is the action <see cref="DrainBatteryAction"/> activated?
    /// If activated the IPC can drain charge from S.M.E.S., APC, Substation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DrainActivated;

    /// <summary>
    /// Movement speed when IPC battery is low.
    /// </summary>
    [DataField]
    public float LowChargeSpeed = 0.2f;

    /// <summary>
    /// Damage taken by IPC from EMP.
    /// </summary>
    [DataField]
    public float DamageFromEmp = 30;

    /// <summary>
    /// The minimum difference from the NormalBodyTemperature
    /// (more or less) at which increased battery discharge begins
    /// and the draw rate becomes equal to <see cref="OverDrawRate"/>.
    /// </summary>
    [DataField]
    public float OverDelta = 20f;

    /// <summary>
    /// The difference from the NormalBodyTemperature
    /// when the battery discharge is maximum and equal to <see cref="CritDrawRate"/>.
    /// </summary>
    [DataField]
    public float CritDelta = 60.0f;

    /// <summary>
    /// Base draw rate battery at NormalBodyTemperature.
    /// </summary>
    [DataField]
    public float BaseDrawRate = 0.8f;

    /// <summary>
    /// Draw rate when difference from NormalBodyTemperature
    /// equal to <see cref="OverDelta"/>.
    /// </summary>
    [DataField]
    public float OverDrawRate = 2.5f;

    /// <summary>
    /// Draw rate when difference from NormalBodyTemperature
    /// equal to <see cref="CritDelta"/>.
    /// </summary>
    [DataField]
    public float CritDrawRate = 5.0f;
}

public sealed partial class ToggleDrainActionEvent : InstantActionEvent;

public sealed partial class OpenIpcFaceActionEvent : InstantActionEvent;
