// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Corruption;

/// <summary>
///  Resecpie for corruption of animals
/// </summary>
[Prototype("corruptedAnimals")]

public sealed partial class CultYoggCorruptedAnimalsPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("corruptedAnimal", required: true)]
    public EntProtoId Start { get; private set; }

    [DataField(required: true)]
    public EntProtoId Result { get; private set; }
}
