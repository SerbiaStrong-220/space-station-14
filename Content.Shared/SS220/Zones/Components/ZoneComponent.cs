// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Zones.Components;

/// <summary>
/// The component used to determine the zones located on the <see cref="ZoneParams.Container"/>.
/// A zone can be used to determine a certain area on the <see cref="ZoneParams.Container"/>
/// in which various events can occur, as well as with entities entering, staying inside, and leaving the zone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    /// Should the size of the zone be attached to the grid
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AttachToLattice;

    /// <summary>
    /// Original size of the zone
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<Box2> Area = [];

    /// <summary>
    /// An array of entities currently located in the zone
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> EnteredEntities = [];
}
