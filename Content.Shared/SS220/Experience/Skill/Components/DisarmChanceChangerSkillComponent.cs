// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisarmChanceChangerSkillComponent : Component
{
    /// <summary>
    /// Applies when skill owner tries to disarm
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public float DisarmByMultiplier;

    /// <summary>
    /// Applies when skill owner is being disarmed
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public float DisarmedMultiplier;
}
