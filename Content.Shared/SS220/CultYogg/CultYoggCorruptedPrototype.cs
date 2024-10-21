// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg
{
    /// <summary>
    ///     Recipes for corruption
    /// </summary>
    [Prototype("corrupted")]
    [Serializable, NetSerializable]
    public sealed partial class CultYoggCorruptedPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        /// Defines entity to which this corruption can be applied
        /// </summary>
        [DataField("from", required: true)]
        public CorruptionInitialEntityUnion FromEntity { get; private set; }

        /// <summary>
        /// Entity prototype to spawn corrupted variant from
        /// </summary>
        [DataField("result", required: true)]
        public ProtoId<EntityPrototype>? Result { get; private set; }

        /// <summary>
        /// Visual effect to spawn when entity corrupted from this recipe gets reversed back
        /// </summary>
        [DataField("corruptionReverseEffect")]
        public ProtoId<EntityPrototype>? CorruptionReverseEffect { get; private set; }

        /// <summary>
        /// Should we empty the storage when it corrpted.
        /// Used to prevent wierd bugs like hardsuits helmet or ammo in guns.
        /// Set "true" if it has a pocket or smt that can make valuable items unreachable
        /// </summary>
        [DataField("emptyStorage", required: false)]
        public bool EmptyStorage { get; private set; }

    }

    [DataDefinition]
    [Serializable, NetSerializable]
    public partial struct CorruptionInitialEntityUnion
    {
        // The properties here are arranged in descending order of priority, so the first one will be checked first.

        /// <summary>
        /// Defines that source entity should be spawned from specified prototype id
        /// </summary>
        [DataField("prototypeId")]
        public ProtoId<EntityPrototype>? PrototypeId { get; private set; }

        /// <summary>
        /// Defines that source entity should be a stack with specified stack type
        /// </summary>
        [DataField("stackType")]
        public ProtoId<StackPrototype>? StackType { get; private set; }

        /// <summary>
        /// Defines that source entity should be spawned from prototype, inheriting the prototype with specified id
        /// </summary>
        [DataField("parentPrototypeId")]
        public ProtoId<EntityPrototype>? ParentPrototypeId { get; private set; }

        /// <summary>
        /// Defines that source entity should be tagged with specified tag
        /// </summary>
        [DataField("tag")]
        public ProtoId<TagPrototype>? Tag { get; private set; }
    }
}
