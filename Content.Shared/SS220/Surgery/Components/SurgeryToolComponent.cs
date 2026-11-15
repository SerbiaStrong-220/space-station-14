// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public SurgeryToolType ToolType = SurgeryToolType.Invalid;

    [DataField]
    [AutoNetworkedField]
    public HashSet<SurgeryToolType> AdditionalToolTypes = new();

    [DataField]
    [AutoNetworkedField]
    public float SpeedModifier = 1f;

    [DataField]
    [AutoNetworkedField]
    public float FailureModifier = 0f;

    [DataField]
    public DamageSpecifier? FailureDamage;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan FailureDamageDelay = TimeSpan.FromSeconds(1f);

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextFailureDamageTime;

    [DataField("sound")]
    public SoundSpecifier? UsingSound;

    [DataField]
    [AutoNetworkedField]
    public bool IsPainless;
}

public enum SurgeryToolType
{
    Invalid = -1,
    Specific,
    Scalpel,
    Retractor,
    Hemostat,
    Saw,
    BoneGel,
    Cautery,
    Bonesetter,
    Drill,
    BloodFilter,
    AutopsyScanner,
    SurgicalTape,
    Shears
}
