// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

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
    public List<EntityUid> enslavedEntities = new();
}
