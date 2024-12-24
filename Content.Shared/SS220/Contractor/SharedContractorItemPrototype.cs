using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is a prototype for...
/// </summary>
[Serializable, NetSerializable, Prototype("contractorItems")]
public sealed partial class SharedContractorItemPrototype : IPrototype
{
    [DataField("items")]
    public Dictionary<string, FixedPoint2> Items = new();

    [IdDataField]
    public string ID { get; } = default!;
}
