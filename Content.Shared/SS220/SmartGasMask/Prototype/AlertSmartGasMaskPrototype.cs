// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SmartGasMask.Prototype;

/// <summary>
/// For selectable actions prototypes in SmartGasMask.
/// </summary>
[Prototype]
public sealed partial class AlertSmartGasMaskPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; set; } = default!;

    [DataField(required: true)]
    public EntProtoId EntityPrototype;

    [DataField]
    public EntProtoId? IconPrototype = null;
}
