// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Detective.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDetectiveCameraAttachSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class DetectiveCameraAttachComponent : Component
{
    [DataField]
    public bool Attached;

    [DataField, AutoNetworkedField]
    public float AttachTime = 3f;

    [DataField, AutoNetworkedField]
    public float DetachTime = 3f;

    [DataField("сellSlotId"), AutoNetworkedField]
    public string CellSlotId = "detective_camera_slot";
}
