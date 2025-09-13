using Content.Shared.Actions;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Kpb;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class KpbComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    [DataField]
    public EntProtoId DrainBatteryAction = "ActionDrainBattery";

    [DataField]
    public EntProtoId ChangeFaceAction = "ActionKpbChangeFace";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public EntityUid? ChangeFaceActionEntity;

    [DataField, AutoNetworkedField]
    public string SelectedFace = string.Empty;

    [DataField, AutoNetworkedField]
    public ProtoId<KpbFaceProfilePrototype> FaceProfile = "DefaultKpbFaces";

    public bool DrainActivated;
}

public sealed partial class ToggleDrainActionEvent : InstantActionEvent
{

}

public sealed partial class OpenKpbFaceActionEvent : InstantActionEvent
{
}
