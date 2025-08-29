// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGo;

/// <summary>
///     Recipes for buildings that MiGo can replace with cult buildings
/// </summary>
[Prototype]
[Serializable, NetSerializable]
public sealed partial class MiGoCapturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Determines which building can be captured
    /// </summary>
    [DataField("from", required: true)]
    public MiGoCaptureInitialEntityUnion FromEntity { get; private set; }

    /// <summary>
    /// Entity prototype to replace targeted building
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ReplacementProto { get; private set; }

    /// <summary>
    /// Local cooldown of the ReplacementProto recipe is individual for each MiGo
    /// Made to prevent MiGos from trying to capture the entire station.
    /// </summary>
    [DataField]
    public TimeSpan ReplacementCooldown { get; private set; } = TimeSpan.FromSeconds(30);
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct MiGoCaptureInitialEntityUnion
{

    /// <summary>
    /// Defines that source entity should be spawned from specified prototype id
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype>? PrototypeId { get; private set; }

    /// <summary>
    /// Defines that source entity should be spawned from prototype, inheriting the prototype with specified id
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype>? ParentPrototypeId { get; private set; }

    /// <summary>
    /// Defines that source entity should be tagged with specified tag
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype>? Tag { get; private set; }
}
