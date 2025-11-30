// Original code by Corvax dev team. all edits done by SS220 dev team.

using Content.Shared.SS220.Experience;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.HiddenDescription;

/// <summary>
/// A component that shows players with specific roles or jobs additional information about entities
/// </summary>

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiddenDescriptionComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<KnowledgePrototype>, LocId> Entries = new();

    /// <summary>
    /// Prioritizing the location of classified information in an inspection
    /// </summary>
    [DataField]
    public int PushPriority = 1;
}
