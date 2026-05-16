// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Doors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class DoorLubedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int SilentUsesLeft = 0;
}
