// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JetPackUseSkillComponent : Component
{
    public uint LastInputTick;

    [DataField]
    [AutoNetworkedField]
    public float FailChance = 0.1f;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan JetPackFailureCooldown = TimeSpan.FromSeconds(2f);

    [DataField]
    [AutoNetworkedField]
    public LocId JetPackFailurePopup = "jet-pack-use-skill-jet-pack-failure";

    [DataField]
    [AutoNetworkedField]
    public float GasUsageModifier = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessAcceleration = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessFriction = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessFrictionNoInput = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessModifier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool JetPackActive;
}
