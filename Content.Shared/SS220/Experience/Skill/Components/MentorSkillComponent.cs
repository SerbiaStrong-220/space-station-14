// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Components;

/// <summary>
/// This is used to stop entity from being disarmed
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MentorSkillComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SkillTreePrototype>, MentorEffectData> TeachInfo;

    [DataField(required: true)]
    public float Range = 4f;

    [DataField]
    public TimeSpan ActivateTimeout = TimeSpan.FromSeconds(4f);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastActivate;
}


