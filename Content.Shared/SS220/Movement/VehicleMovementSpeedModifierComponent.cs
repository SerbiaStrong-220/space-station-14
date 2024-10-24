using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Movement
{
    /// <summary>
    /// Applies basic movement speed and movement modifiers for an vehicle.
    /// If this is not present on the entity then they will use defaults for movement.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(MovementSpeedModifierSystem))]
    public sealed partial class VehicleMovementSpeedModifierComponent : Component
    {
        public const float DefaultFriction = 1f;
        public const float DefaultFrictionNoInput = 6f;
        public const float DefaultAcceleration = 1f;

        /// <summary>
        /// The acceleration applied to mobs when moving.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
        public float Acceleration = DefaultAcceleration;

        /// <summary>
        /// The negative velocity applied for friction.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
        public float Friction = DefaultFriction;

        /// <summary>
        /// The negative velocity applied for friction.
        /// </summary>
        [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite), DataField]
        public float? FrictionNoInput;
    }
}
