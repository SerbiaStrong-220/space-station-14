// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Mech.Components;

/// <summary>
/// Attached to entities piloting a <see cref="MechComponent"/>
/// </summary>
/// <remarks>
/// Get in the robot, Shinji
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AltMechPilotComponent : Component
{
    /// <summary>
    /// The mech being piloted
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid Mech;
}
