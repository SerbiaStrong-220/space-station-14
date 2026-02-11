using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.SS220.Damage.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared.SS220.Damage.Components
{
    /// <summary>
    /// Component for damage attempts that changes damage values based on armor coefficients.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(DamageableSystem))]
    public sealed partial class ArmorPenetrationComponent : Component
    {
        [DataField("rules")]
        public List<ArmorPenetrationRule> Rules = new();
    }

    /// <summary>
    /// Defines a rule for how a specific damage type interacts with armor based on its coefficient.
    /// </summary>
    [DataDefinition]
    public partial struct ArmorPenetrationRule
    {
        /// <summary>
        /// The damage type this rule applies to (e.g., Blunt, Piercing).
        /// </summary>
        [DataField("damageType", required: true)]
        public string DamageType { get; set; } = default!;

        /// <summary>
        /// The threshold value for the armor coefficient.
        /// </summary>
        [DataField("armorthreshold")]
        public float ArmorThreshold { get; set; } = 1.0f;

        /// <summary>
        /// The multiplier applied to this damage type if the condition (based on threshold and reversed flag) is met.
        /// </summary>
        [DataField("multiplier")]
        public float Multiplier { get; set; } = 1.0f;

        /// <summary>
        /// If true, the condition is inverted. The multiplier applies if the armor coefficient is GREATER than the threshold.
        /// If false (default), the multiplier applies if the armor coefficient is LESS or equal to the threshold.
        /// </summary>
        [DataField("reversed")]
        public bool Reversed { get; set; } = false;
    }
}
