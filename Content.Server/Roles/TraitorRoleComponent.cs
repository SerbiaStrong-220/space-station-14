using Content.Shared.Roles;

namespace Content.Server.Roles;

/// <summary>
///     Added to mind role entities to tag that they are a syndicate traitor.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorRoleComponent : BaseMindRoleComponent
{
    //ss220 time of assignment on traitor for conditions start
    [DataField]
    public TimeSpan? TimeOfAssignment;
    //ss220 time of assignment on traitor for conditions end
}
