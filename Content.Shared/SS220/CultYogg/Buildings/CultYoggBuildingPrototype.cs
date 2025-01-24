// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.Buildings;

[Prototype("cultYoggBuilding")]
[Serializable, NetSerializable]
public sealed partial class CultYoggBuildingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Final product of this recipe.
    /// </summary>
    [DataField("result", required: true)]
    public EntProtoId ResultProtoId { get; private set; }

    /// <summary>
    /// Intermediate form of the building, where cultists should put <see cref="Materials"/> to build
    /// <see cref="ResultProtoId"/>. If not present, <see cref="ResultProtoId"/> will be spawned instantly.
    /// </summary>
    [DataField("frame")]
    public EntProtoId? FrameProtoId { get; private set; }

    /// <summary>
    /// Order to sort these buiildings in the UI.
    /// </summary>
    [DataField("order")]
    public int Order { get; private set; } = 0;

    /// <summary>
    /// Optional cooldown to build anything after that, default action cooldown will be used if not present.
    /// </summary>
    [DataField("cooldown")]
    public TimeSpan? CooldownOverride { get; private set; }

    /// <summary>
    /// Set of materials that should be put to the <see cref="FrameProtoId"/> to get <see cref="ResultProtoId"/>.
    /// </summary>
    [DataField("materials")]
    public List<CultYoggBuildingMaterial> Materials { get; private set; } = [];
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct CultYoggBuildingMaterial
{
    [DataField("stack", required: true)]
    public ProtoId<StackPrototype> StackType;
    [DataField("count", required: true)]
    public int Count;
    [DataField("icon", required: true)]
    public SpriteSpecifier Icon;
}
