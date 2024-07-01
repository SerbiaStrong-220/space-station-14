using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg
{
    /// <summary>
    ///     Recepies for corruption
    /// </summary>
    [Prototype("corrupted")]

    [Serializable, NetSerializable]
    public sealed partial class CultYoggCorruptedPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("corruptedEntity")]
        public string? Start { get; private set; }

        [DataField("result")]
        public string? Result { get; private set; }
    }
}
