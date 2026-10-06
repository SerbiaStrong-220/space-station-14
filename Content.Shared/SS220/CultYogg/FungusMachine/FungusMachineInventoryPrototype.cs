// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.FungusMachine;

[Prototype]
public sealed partial class FungusMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("startingInventory")]
    public Dictionary<EntProtoId, uint> StartingInventory { get; private set; } = new();
}
