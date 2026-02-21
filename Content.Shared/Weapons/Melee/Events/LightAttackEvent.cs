using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised when a light attack is made.
/// </summary>
[Serializable, NetSerializable]
public sealed class LightAttackEvent : AttackEvent
{
    public readonly NetEntity? Target;
    public readonly NetEntity Weapon;

    public LightAttackEvent(NetEntity? target, NetEntity weapon, NetCoordinates coordinates) : base(coordinates)
    {
        Target = target;
        Weapon = weapon;
    }
}

// SS220-MartialArts-Start
// Why:
// LightAttackEvent is used only for internal use in MeleeWeaponSystem, prediction and etc,
// it raises before any checks is performed at server side not even talking about of applying effects
// so we can't rely on it to add new mechanics based on melee

// ... the weapon system needs refactor af

/// <summary>
/// Raised on user when a light attack is made and handled by melee weapon system,
/// used for other systems to add new logic based on melee
/// </summary>
public sealed partial class LightAttackPerformedEvent : EntityEventArgs
{
    public readonly EntityUid? Target;
    public readonly EntityUid Weapon;
    public readonly EntityCoordinates Coordinates;

    public LightAttackPerformedEvent(EntityUid? target, EntityUid weapon, EntityCoordinates coordinates)
    {
        Target = target;
        Weapon = weapon;
        Coordinates = coordinates;
    }
};
// SS220-MartialArts-End
