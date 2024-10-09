using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.FastUI;

[Prototype("secretListing")]
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SecretListingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; set; } = default!;

    [DataField("title", required: true)]
    public string Title = default!;

    [DataField("description", required: true)]
    public string Description = default!;

    [DataField("subDescription")]
    public string SubDescription = default!;

    [DataField("buttonText")]
    public string ButtonText = default!;

    [DataField("buttonState")]
    public Enum ButtonState = default!;

    [DataField("icon")]
    public SpriteSpecifier? Icon = null;
}
