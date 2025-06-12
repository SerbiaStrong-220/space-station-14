// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

/// <summary>
/// The component used to determine the zones located on the <see cref="ZoneParams.Container"/>.
/// A zone can be used to determine a certain area on the <see cref="ZoneParams.Container"/>
/// in which various events can occur, as well as with entities entering, staying inside, and leaving the zone.
/// </summary>
[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZoneComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public ZoneParams ZoneParams = new();

    /// <summary>
    /// An array of entities located in the zone
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> Entities = new();
}
