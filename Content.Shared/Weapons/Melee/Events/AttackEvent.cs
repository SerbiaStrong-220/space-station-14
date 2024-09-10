using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.Events
{
    [Serializable, NetSerializable]
    public abstract class AttackEvent : EntityEventArgs
    {
        /// <summary>
        /// Coordinates being attacked.
        /// </summary>
        public readonly NetCoordinates Coordinates;

        protected AttackEvent(NetCoordinates coordinates)
        {
            Coordinates = coordinates;
        }
    }

    /// <summary>
    ///     Event raised on entities that have been attacked.
    /// </summary>
    public sealed class AttackedEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity used to attack, for broadcast purposes.
        /// </summary>
        public EntityUid Used { get; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public EntityCoordinates ClickLocation { get; }

        public DamageSpecifier BonusDamage = new();

        public AttackedEvent(EntityUid used, EntityUid user, EntityCoordinates clickLocation)
        {
            Used = used;
            User = user;
            ClickLocation = clickLocation;
        }

        // SS220 hook attack event start
        public bool Cancelled = false;
        // SS220 hook attack event end
    }
    //ss220 extended weapon logic start
    /// <summary>
    ///     SS220 Event raised on user for extended melee weapon logic.
    /// </summary>
    public sealed class WeaponAttackEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public EntityUid User { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public EntityUid Target { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>

        public WeaponAttackEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
    //ss220 extended weapon logic end
}
