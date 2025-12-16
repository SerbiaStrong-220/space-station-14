// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Components;

/// <summary>
/// When owner speaks near entities it gives them buff for studying progress of skills
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MentorRoleComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SkillTreePrototype>, MentorEffectData> TeachInfo;

    [DataField]
    public float Range = 4f;

    [DataField]
    public TimeSpan ActivateTimeout = TimeSpan.FromSeconds(4f);

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastActivate;
}


