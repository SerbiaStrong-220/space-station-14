// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.MiGo;

[Prototype("cultYoggSeeds")]
[Serializable, NetSerializable]
public sealed partial class CultYoggSeedsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// What type of seed we will plant
    /// </summary>
    [DataField("seed", required: true)]
    public EntProtoId SeedProtoId { get; private set; }

    /// <summary>
    /// What type of plant we will get in result
    /// Required to see result
    /// </summary>
    [DataField("plant")]
    public EntProtoId PlantProtoId { get; private set; }
}
