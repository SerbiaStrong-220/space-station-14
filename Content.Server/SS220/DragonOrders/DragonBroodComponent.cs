// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.DragonOrders;

/// <summary>
/// Marks a member of a dragon's brood, which follows the orders from <see cref="DragonOrdersComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(DragonOrdersSystem))]
public sealed partial class DragonBroodComponent : Component
{
    /// <summary>
    /// The dragon this brood member obeys. Null until some dragon adopts it.
    /// </summary>
    [DataField]
    public EntityUid? Dragon;
}
