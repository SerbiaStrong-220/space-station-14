using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DoorLubedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Remaining = 0;
}
