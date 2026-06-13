// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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

    [DataField] public EntityUid? PilotUiActionEntity;
    [DataField] public EntityUid? PilotEjectActionEntity;

    [DataField]
    public EntProtoId PilotUiAction = "ActionPilotOpenUI";//Why? Because mech and pilot couldn't have same actions so or this or adding/deleting actions aaaall the way

    [DataField]
    public EntProtoId PilotEjectAction = "ActionPilotEject";
}
