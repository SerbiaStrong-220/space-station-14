// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

/// <summary>
/// The component used to determine the zones located on the <see cref="Container"/>.
/// A zone can be used to determine a certain area on the <see cref="Container"/>
/// in which various events can occur, as well as with entities entering, staying inside, and leaving the zone.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZoneComponent : Component
{
    /// <summary>
    /// The entity that this zone is assigned to.
    /// Used to determine local coordinates
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? Container;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Color? CurColor;

    /// <summary>
    /// Default color of the zone
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color DefaultColor = Color.Red;

    /// <summary>
    /// Boxes in local coordinates (attached to <see cref="Container"/>) that determine the size of the zone
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<Box2> Boxes = new();

    /// <summary>
    /// An array of entities located in the zone
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Entities = new();
}
