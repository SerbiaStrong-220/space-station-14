
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Prototypes;

[Prototype("virusSpread")]
public sealed partial class VirusSpreadPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float Infectivity = 0.0f;
}
