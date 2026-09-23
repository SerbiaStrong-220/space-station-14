// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
