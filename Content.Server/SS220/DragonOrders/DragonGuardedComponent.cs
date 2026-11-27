// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.DragonOrders;

/// <summary>
/// Marks an entity that some dragon's brood is guarding.
/// </summary>
[RegisterComponent, Access(typeof(DragonOrdersSystem))]
public sealed partial class DragonGuardedComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Dragons = new();
}
