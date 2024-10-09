// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg;

[Prototype("cultYoggBuilding")]
[Serializable, NetSerializable]
public sealed partial class CultYoggBuildingPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("frame", required: true)]
    public ProtoId<EntityPrototype> FrameEntityId { get; private set; }

    [DataField("order")]
    public int Order { get; private set; } = 0;

    [DataField("result", required: true)]
    public ProtoId<EntityPrototype> ResultEntityId { get; private set; }

    [DataField("materials", required: true)]
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
