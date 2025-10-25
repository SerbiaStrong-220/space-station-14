using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedPuddleSystem))]
    public sealed partial class PuddleComponent : Component
    {
        [DataField]
        public SoundSpecifier SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [DataField]
        public FixedPoint2 OverflowVolume = FixedPoint2.New(20);

        [DataField("solution")] public string SolutionName = "puddle";

        /// <summary>
        /// Default minimum speed someone must be moving to slip for all reagents.
        /// </summary>
        [DataField]
        public float DefaultSlippery = 5.5f;

        [ViewVariables]
        public Entity<SolutionComponent>? Solution;

        //ss220 add additional info for round start
        /// <summary>
        /// Stores the last user entity who interacted with this entity
        /// </summary>
        [ViewVariables]
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public EntityUid? LastInteractionUser;
        //ss220 add additional info for round end
    }
}
