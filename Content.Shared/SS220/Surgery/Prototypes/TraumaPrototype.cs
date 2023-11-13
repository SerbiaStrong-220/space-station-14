
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Surgery.Prototypes
{
    [Prototype("trauma")]
    public sealed partial class TraumaPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField(required: true)]
        public LocId Name = string.Empty;

        /// <summary>
        /// Used for UI representation
        /// </summary>
        [DataField(required: true)]
        public SpriteSpecifier Icon = default!;

        [DataField("injures")]
        public List<ProtoId<DamageTypePrototype>> Injures = new();
    }
}