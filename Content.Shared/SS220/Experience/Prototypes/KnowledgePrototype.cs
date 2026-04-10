// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed class KnowledgePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Some knowledges should give access to other knowledge's information, but should really be shown to player
    /// </summary>
    [DataField]
    public HashSet<ProtoId<KnowledgePrototype>> AdditionalKnowledges = new();

    [DataField(required: true)]
    public LocId KnowledgeName = default;

    [DataField(required: true)]
    public LocId KnowledgeDescription = default;

    [DataField]
    public LocId? MessageOnAcquiring = null;

    [DataField]
    public LocId? MessageOnLosing = null;
}
