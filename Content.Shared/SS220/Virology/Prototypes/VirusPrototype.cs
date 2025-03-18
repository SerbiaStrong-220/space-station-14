
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Prototypes;

[Prototype("virus")]
public sealed partial class VirusPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set } = default!;
}
