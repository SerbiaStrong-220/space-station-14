using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is a prototype for contractor items in uplink.
/// </summary>
[Serializable, NetSerializable, Prototype("contractorItems")]
public sealed partial class SharedContractorItemPrototype : IPrototype
{
    [DataField("items")]
    public Dictionary<string, ContractorItemData> Items = new();

    [IdDataField]
    public string ID { get; } = default!;
}

[Serializable]
[NetSerializable]
[DataDefinition]
public sealed partial class ContractorItemData
{
    [DataField]
    public FixedPoint2 Amount;

    [DataField]
    public int? Quantity;
}
