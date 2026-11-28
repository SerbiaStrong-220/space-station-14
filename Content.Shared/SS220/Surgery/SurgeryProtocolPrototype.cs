// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery;

/// <summary>
/// A protocol of a surgery disk: with the disk in an operating computer the graph steps that require the protocol open on the linked table.
/// </summary>
[Prototype]
public sealed partial class SurgeryProtocolPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
