using Content.Shared.Roles;

namespace Content.Server.SS220.Roles;

/// <summary>
///     Added to mind entities to tag that they are a Revolutionary.
/// </summary>

[RegisterComponent, ExclusiveAntagonist]
public sealed partial class CultYoggRoleComponent : AntagonistRoleComponent
{
}
