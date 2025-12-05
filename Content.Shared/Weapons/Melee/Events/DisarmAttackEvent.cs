using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events;

[Serializable, NetSerializable]
public sealed class DisarmAttackEvent : AttackEvent
{
    public NetEntity? Target;

    public DisarmAttackEvent(NetEntity? target, NetCoordinates coordinates) : base(coordinates)
    {
        Target = target;
    }
}

// SS220-MartialArts-Start
// Why:
// DisarmAttackEvent is used only for internal use in MeleeWeaponSystem, prediction and etc,
// it raises before any checks is performed at server side not even talking about of applying effects
// so we can't rely on it to add new mechanics based on melee

/// <summary>
/// Raised on user when a disarm is made and handled by melee weapon system,
/// used for other systems to add new logic based on melee
/// </summary>
public sealed partial class DisarmAttackPerformedEvent : EntityEventArgs
{
    public EntityUid? Target;
    public EntityCoordinates Coordinates;

    public DisarmAttackPerformedEvent(EntityUid? target, EntityCoordinates coordinates)
    {
        Target = target;
        Coordinates = coordinates;
    }
};
// SS220-MartialArts-End
