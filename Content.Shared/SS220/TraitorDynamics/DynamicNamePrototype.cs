using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TraitorDynamics;


[Prototype]
public sealed partial class DynamicNamePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public List<LocId> ListNames = new();
}
