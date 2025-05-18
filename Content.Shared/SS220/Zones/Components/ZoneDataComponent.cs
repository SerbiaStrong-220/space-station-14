
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ZonesDataComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public HashSet<NetEntity> Zones = new();
}
