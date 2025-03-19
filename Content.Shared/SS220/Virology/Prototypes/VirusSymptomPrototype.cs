using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Prototypes;

[Prototype("virusSymptom")]
public sealed partial class VirusSymptomPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Visibility = 0.0f;
}
