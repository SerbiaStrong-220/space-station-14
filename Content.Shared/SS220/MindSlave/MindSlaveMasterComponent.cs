// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MindSlave;

/// <summary>
/// Component, used to mark the master of some enslaved minds.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindSlaveMasterComponent : Component
{
    /// <summary>
    /// List of all enslaved entities, which were enslaved by the owner.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public List<EntityUid> EnslavedEntities = new();

    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "MindSlaveMasterIcon";

    public bool IconVisibleToGhost { get; set; } = false;
}
