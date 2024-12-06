// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;

namespace Content.Server.SS220.Roles;

[RegisterComponent]
public sealed partial class SpiderQueenRoleComponent : BaseMindRoleComponent
{
    [ViewVariables]
    public bool IsCreateCocoonsCompletedOnce = false;
}
