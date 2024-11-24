// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Server.SS220.MindSlave.Components;

[RegisterComponent]
public sealed partial class MindSlaveDisfunctionComponent : Component
{

    [ViewVariables]
    public Dictionary<MindSlaveDisfunctionType, List<string>> Disfunction => DisfunctionParameters.Disfunction;

    [ViewVariables]
    public DamageSpecifier DeadlyStageDamage => DisfunctionParameters.DeadlyStageDamage;

    [DataField(required: true)]
    public DisfunctionParameters DisfunctionParameters = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public List<IComponent> DisfunctionComponents = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public MindSlaveDisfunctionType DisfunctionStage = MindSlaveDisfunctionType.None;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Active = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Deadly = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Weakened = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextProgressTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextDeadlyDamageTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PausedTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ConstMinutesBetweenStages = 35;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxRandomMinutesBetweenStages = 7;

}

public enum MindSlaveDisfunctionType
{
    None = 0,
    Initial,
    Progressive,
    Terminal,
    Deadly
}
