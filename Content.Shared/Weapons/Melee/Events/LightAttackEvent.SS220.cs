using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates) : AttackEvent(coordinates)
{
    public readonly NetEntity? Target = target;
    public readonly NetEntity Weapon = weapon;
}
