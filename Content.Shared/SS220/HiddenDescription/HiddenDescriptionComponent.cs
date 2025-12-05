// Original code by Corvax dev team. all edits done by SS220 dev team.

using Content.Shared.SS220.Experience;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.HiddenDescription;

/// <summary>
/// A component that changes entity names and adds description according to their Knowledge
/// </summary>

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiddenDescriptionComponent : Component
{
    [AutoNetworkedField]
    [DataField(required: true)]
    public Dictionary<ProtoId<KnowledgePrototype>, LocId> Entries = new();

    /// <summary>
    /// If this field is null, that mean we skip any renaming because entity do itself
    /// </summary>
    [AutoNetworkedField]
    [DataField(required: true)]
    public LocId? HiddenName;

    /// <summary>
    /// Uses to define name overrides and their order. Null goes for original name <br/>
    /// yml goes: <br/>
    /// my_list: <br/>
    ///   - [protoId1, LocId1] <br/>
    ///   - [protoId2, LocId2] <br/>
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<(ProtoId<KnowledgePrototype>, LocId?)> NameEntries = new();

    /// <summary>
    /// Prioritizing the location of classified information in an inspection
    /// </summary>
    [DataField]
    public int PushPriority = 1;
}
