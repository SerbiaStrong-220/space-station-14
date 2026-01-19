// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
// TODO
public sealed partial class JetPackUseSkillComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public float FailChance = 0.1f;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan JetPackFailureTime = TimeSpan.FromSeconds(2f);

    [DataField]
    [AutoNetworkedField]
    public LocId JetPackFailurePopup = "jet-pack-use-skill-jet-pack-failure";

    [DataField]
    [AutoNetworkedField]
    public float GasUsageModifier = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessAcceleration;

    [DataField, AutoNetworkedField]
    public float WeightlessFriction;

    [DataField, AutoNetworkedField]
    public float WeightlessFrictionNoInput;

    [DataField, AutoNetworkedField]
    public float WeightlessModifier;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool JetPackActive;
}
