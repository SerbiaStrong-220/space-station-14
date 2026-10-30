// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.Skill.Components;

/// <summary>
/// This is used to stop entity from being disarmed
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisarmBlockSkillComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public LocId DisarmBlockedPopupItem = "disarm-blocked-popup-item";

    [DataField]
    [AutoNetworkedField]
    public LocId DisarmBlockedPopupHand = "disarm-blocked-popup-hand";
}
