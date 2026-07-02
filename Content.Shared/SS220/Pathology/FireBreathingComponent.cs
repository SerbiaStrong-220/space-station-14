// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FireBreathingComponent : Component
{
    [DataField]
    public EntProtoId FireballProto = "ProjectileVirusFireball";

    [DataField]
    public float FireballSpeed = 12f;

    /// <summary>How far in front of the caster the fireball spawns, so it doesn't clip the caster.</summary>
    [DataField]
    public float SpawnOffset = 1.5f;

    /// <summary>Minimum aim distance squared; a shorter aim cancels the breath.</summary>
    [DataField]
    public float AimDeadzoneSq = 0.01f;

    /// <summary>Fire stacks caster sets on himself with every use.</summary>
    [DataField]
    public float SelfFireStacks = 4f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");

    [DataField]
    public EntProtoId ActionId = "ActionVirusFireBreathing";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}

public sealed partial class FireBreathingActionEvent : WorldTargetActionEvent { }
