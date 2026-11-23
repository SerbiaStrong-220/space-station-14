using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.FastUI;

[Prototype]
[DataDefinition]
public sealed partial class SecretListingCategoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField(required: true)]
    public string WindowName { get; set; } = string.Empty;

    [DataField(required: true)]
    public string WindowDescription { get; set; } = string.Empty;

    [DataField]
    public HashSet<ProtoId<SecretListingPrototype>> Listings { get; set; } = new();
}
