using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DarkForces.Ratvar.Prototypes;

[Serializable, Prototype("ratvarCraft")]
public sealed partial class RatvarCraftReceiptPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public int BrassCost;

    [DataField(required: true)]
    public int PowerCost;

    [DataField(required: true)]
    public int CraftingTime;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField(required: true)]
    public EntProtoId EntityProduce;
}

[Serializable, Prototype("ratvarCraftCategory")]
public sealed partial class RatvarCraftCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public List<ProtoId<RatvarCraftReceiptPrototype>> Receipts = new();
}
