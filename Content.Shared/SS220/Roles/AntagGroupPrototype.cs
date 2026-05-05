// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Roles;

[Prototype]
public sealed partial class AntagGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

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
