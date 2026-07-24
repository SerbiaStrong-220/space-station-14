using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
//SS220-MicroFixesIPC bedin
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
//SS220-MicroFixesIPC end

namespace Content.Shared.Bed.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
    public sealed partial class HealOnBuckleComponent : Component
    {
        /// <summary>
        /// Damage to apply to entities that are strapped to this entity.
        /// </summary>
        [DataField(required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        /// How frequently the damage should be applied, in seconds.
        /// </summary>
        [DataField(required: false)]
        public float HealTime = 1f;

        /// <summary>
        /// Damage multiplier that gets applied if the entity is sleeping.
        /// </summary>
        [DataField]
        public float SleepMultiplier = 3f;

        /// <summary>
        /// Next time that <see cref="Damage"/> will be applied.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
        public TimeSpan NextHealTime = TimeSpan.Zero; //Next heal

        /// <summary>
        /// Action for the attached entity to be able to sleep.
        /// </summary>
        [DataField, AutoNetworkedField]
        public EntityUid? SleepAction;

        //SS220-MicroFixesIPC begin
        /// <summary>
        /// Species will not receive healing from this bed.
        /// </summary>
        [DataField]
        public List<ProtoId<SpeciesPrototype>> SpeciesBlacklist = [];
        //SS220-MicroFixesIPC end
    }
}
