using Robust.Shared.Map;

namespace Content.Shared.Weapons.Melee.Events;

// Why:
// DisarmAttackEvent is used only for internal use in MeleeWeaponSystem, prediction and etc,
// it raises before any checks is performed at server side not even talking about of applying effects
// so we can't rely on it to add new mechanics based on melee

/// <summary>
/// Raised on user when a disarm is made and handled by melee weapon system,
/// used for other systems to add new logic based on melee
/// </summary>
public sealed partial class DisarmAttackPerformedEvent(EntityUid? target, EntityCoordinates coordinates) : EntityEventArgs
{
    public EntityUid? Target = target;
    public EntityCoordinates Coordinates = coordinates;
};
