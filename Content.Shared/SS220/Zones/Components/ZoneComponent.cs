
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZoneComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? Parent;

    [ViewVariables, AutoNetworkedField]
    public Color? CurColor;

    [DataField, AutoNetworkedField]
    public Color DefaultColor = Color.Red;

    [ViewVariables, AutoNetworkedField]
    public HashSet<Box2> Boxes = new();

    [ViewVariables]
    public HashSet<EntityUid> Entities = new();
}
