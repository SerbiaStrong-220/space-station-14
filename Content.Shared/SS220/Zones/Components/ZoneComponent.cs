// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Zones.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedZonesSystem), Other = AccessPermissions.ReadExecute)]
[EntityCategory("Zones")]
public sealed partial class ZoneComponent : Component
{
    /// <summary>
    /// Current color of the zone
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Gray;

    /// <summary>
    /// Original size of the zone
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2> Area = [];

    /// <summary>
    /// An array of entities located in the zone
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> LocatedEntities = [];
}
