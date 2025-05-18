
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
public sealed partial class ZoneComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? Parent;

    [ViewVariables, AutoNetworkedField]
    public Color Color = Color.Red;

    [ViewVariables, AutoNetworkedField]
    public HashSet<Box2> Boxes = new();
}
