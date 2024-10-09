using Content.Shared.Materials;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Craft;

[Prototype("narsiCultReceiptCategory")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class NarsiCultCraftReceiptCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField(required: true)]
    public string Title = default!;

    [DataField(required: true)]
    public Dictionary<string, NarsiCultCraftReceipt> Items = new();
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class NarsiCultCraftReceipt
{
    [DataField(required: true)]
    public string Name = default!;

    [DataField(required: true)]
    public string Description = default!;

    [DataField(required: true)]
    public string Icon = default!;

    [DataField( required: true)]
    public string ItemToSpawn = default!;

    [DataField(required: true)]
    public ProtoId<MaterialPrototype> RequiredMaterial;

    [DataField]
    public string ButtonText = "Выковать";

    [DataField(required: true)]
    public int Cost;
}
