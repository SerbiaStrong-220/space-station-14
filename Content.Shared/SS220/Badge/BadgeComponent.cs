// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Badge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BadgeComponent : Component
{
    [DataField]
    public string Label = "badge-label-security";

    [DataField]
    public string Department = "badge-department-security";

    [DataField]
    public string Color = "gold";

    [DataField, AutoNetworkedField]
    public string? FullName;

    [DataField, AutoNetworkedField]
    public string? JobName;

    [DataField, AutoNetworkedField]
    public string? SerialNumber;
}
