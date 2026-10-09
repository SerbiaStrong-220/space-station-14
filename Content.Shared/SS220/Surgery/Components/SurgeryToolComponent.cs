// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public SurgeryToolType ToolType = SurgeryToolType.Invalid;

    [DataField]
    public DamageSpecifier? FailureDamage;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan FailureDamageDelay = TimeSpan.FromSeconds(1f);

    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public TimeSpan NextFailureDamageTime;

    [DataField("sound")]
    public SoundSpecifier? UsingSound = null;
}

// for now I need only this ones
public enum SurgeryToolType
{
    Invalid = -1,
    Specific,
    Scalpel,
    Retractor,
    Hemostat,
    Saw,
    BoneGel,
    Cautery
}
