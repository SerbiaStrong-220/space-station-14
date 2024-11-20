// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MindSlave;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindSlaveRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// Enslaved person's master, which he obeys to.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? masterEntity;

    /// <summary>
    /// Enslaved person's objective.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? objectiveEntity;
}
