// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGo;

/// <summary>
/// </summary>
[Prototype]
public sealed partial class MiGoCapturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("from", required: true)]
    public MiGoCaptureInitialEntityUnion FromEntity { get; private set; }

    [DataField(required: true)]
    public EntProtoId ReplacementProto { get; private set; }

    [DataField]
    public TimeSpan ReplacementTime { get; private set; } = TimeSpan.FromSeconds(30);
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct MiGoCaptureInitialEntityUnion
{
    [DataField("prototypeId")]
    public ProtoId<EntityPrototype>? PrototypeId { get; private set; }

    [DataField("parentPrototypeId")]
    public ProtoId<EntityPrototype>? ParentPrototypeId { get; private set; }

    [DataField("tag")]
    public ProtoId<TagPrototype>? Tag { get; private set; }
}
