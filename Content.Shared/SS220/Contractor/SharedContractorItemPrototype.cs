using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is a prototype for...
/// </summary>
[Serializable, NetSerializable, Prototype("contractorItems")]
public sealed partial class SharedContractorItemPrototype : IPrototype
{
    [DataField("items", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<FixedPoint2, EntityPrototype>))] //TODO: Delete serializer
    public Dictionary<string, FixedPoint2> Items = new();

    [IdDataField]
    public string ID { get; } = default!;
}
