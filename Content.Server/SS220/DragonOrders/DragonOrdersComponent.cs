// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.NPC;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DragonOrders;

/// <summary>
/// Lets a dragon give orders to its brood, see <see cref="DragonBroodComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(DragonOrdersSystem))]
public sealed partial class DragonOrdersComponent : Component
{
    /// <summary>
    /// The brood stays near this entity and protects it. Null means the dragon itself.
    /// Mirrored into the brood's <see cref="NPCBlackboard.FollowTarget"/>.
    /// </summary>
    [DataField]
    public EntityUid? GuardTarget;

    /// <summary>
    /// The brood attacks this entity until it is dead or gone, then goes back to guarding.
    /// Mirrored into the brood's <see cref="DragonOrdersSystem.DestroyTargetKey"/>.
    /// </summary>
    [DataField]
    public EntityUid? DestroyTarget;

    /// <summary>
    /// Brood without a living dragon within this range joins this dragon.
    /// </summary>
    [DataField]
    public float AdoptRange = 15f;

    [DataField]
    public List<EntProtoId> Actions = new() { "ActionDragonOrderDestroy", "ActionDragonOrderGuard" };
}
