
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Virology.Prototypes;

[Prototype("virus")]
public sealed partial class VirusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VirusSymptomPrototype>))]
    public List<string> Symptoms = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VirusSpreadPrototype>))]
    public List<string> Spread = new();

    [DataField]
    public string DNA = string.Empty;
}
