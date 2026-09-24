// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed partial class KnowledgePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Some knowledges should give access to other knowledge's information, but should really be shown to player
    /// </summary>
    [DataField]
    public HashSet<ProtoId<KnowledgePrototype>> AdditionalKnowledges { get; private set; } = new();

    [DataField(required: true)]
    public LocId KnowledgeName { get; private set; } = default;

    [DataField(required: true)]
    public LocId KnowledgeDescription { get; private set; } = default;

    [DataField]
    public LocId? MessageOnAcquiring { get; private set; } = null;

    [DataField]
    public LocId? MessageOnLosing { get; private set; } = null;
}
