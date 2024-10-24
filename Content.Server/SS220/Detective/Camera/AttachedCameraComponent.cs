// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Detective.Camera;

[RegisterComponent]
[Access(typeof(AttachedCameraSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.ReadWriteExecute)]
public sealed partial class AttachedCameraComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid AttachedCamera;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? UserOwner;

    [ViewVariables, AutoNetworkedField]
    public string CellSlotId;
}
