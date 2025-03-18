using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Prototypes;

[Prototype("virusSymptom")]
public sealed partial class VirusSymptomsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
