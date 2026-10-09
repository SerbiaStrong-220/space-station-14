using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.SS220.ShuttleGunControl;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShuttleGunControlComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public MapCoordinates? LastRotateToPoint;

    [DataField]
    [AutoNetworkedField]
    public float LastRotateToPointRadius = 1.5f;

    [DataField]
    [AutoNetworkedField]
    public float GunRadius = 1f;

    [DataField]
    [AutoNetworkedField]
    public Dictionary<NetEntity, Angle> ShuttleGunRecords = new();
}
