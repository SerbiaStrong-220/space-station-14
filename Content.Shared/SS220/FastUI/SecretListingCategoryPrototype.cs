using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.SS220.FastUI;

[Prototype("secretListingCategory")]
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SecretListingCategoryPrototype : IPrototype, ICloneable
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

    public object Clone()
    {
        return new SecretListingCategoryPrototype
        {
            WindowName = WindowName,
            WindowDescription = WindowDescription,
            Listings = Listings
        };
    }
}
