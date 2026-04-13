// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Components;

/// <summary>
/// This is used to stop entity from being disarmed
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PathologyHealerRemoveDamageSkillComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<ProtoId<PathologyPrototype>, float> RemoveDamageModifier = new();
}
