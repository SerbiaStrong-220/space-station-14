
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ZoneComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? AttachedGrid;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int GridZoneId;
}
