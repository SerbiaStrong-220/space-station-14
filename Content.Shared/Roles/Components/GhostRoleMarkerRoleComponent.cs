using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a ghostrole.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostRoleMarkerRoleComponent : BaseMindRoleComponent
{
    // ss220 add arena start
    /// <summary>
    /// If false, this ghost role is hidden from the round-end summary manifest.
    /// </summary>
    [DataField]
    public bool ShowInSummary = true;
    // ss220 add arena end
}
