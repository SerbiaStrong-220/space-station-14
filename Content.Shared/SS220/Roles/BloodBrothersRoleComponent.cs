using Content.Shared.Roles.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodBrothersRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    /// Another Brother MIND entity
    /// </summary>
    [DataField]
    public EntityUid? Brother;
}
