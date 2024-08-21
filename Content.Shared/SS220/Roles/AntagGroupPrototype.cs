// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Roles;

[Prototype("antagGroup")]
[Serializable, NetSerializable]
public sealed partial class AntagGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// A color representing this antag group to use for text.
    /// </summary>
    [DataField(required: true)]
    public Color Color = Color.Red;

    /// <summary>
    /// List of AntagPrototypes in this group
    /// </summary>
    [DataField]
    public List<ProtoId<AntagPrototype>> Roles = new();
}
