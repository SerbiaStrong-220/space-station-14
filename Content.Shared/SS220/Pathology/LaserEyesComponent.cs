// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LaserEyesComponent : Component
{
    /// <summary>The symptom this tracks, to read the current stage.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "LaserEyes";

    /// <summary>Proto fired, its damage is overridden per stage.</summary>
    [DataField]
    public EntProtoId LaserProto = "BulletLaser";

    [DataField]
    public float LaserSpeed = 20f;

    /// <summary>How far in front of the caster the bolt spawns, so it doesn't clip the caster.</summary>
    [DataField]
    public float SpawnOffset = 1f;

    /// <summary>Minimum aim distance squared; a shorter aim cancels the shot.</summary>
    [DataField]
    public float AimDeadzoneSq = 0.01f;

    /// <summary>Laser damage per stage; the fired bolt is set to this.</summary>
    [DataField]
    public List<DamageSpecifier> DamagePerStage = new();

    /// <summary>Shots needed to reach full eye damage (welding-flash blindness).</summary>
    [DataField]
    public int ShotsToMax = 18;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser.ogg");

    [DataField]
    public EntProtoId ActionId = "ActionVirusLaserEyes";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    // red eye glow — energy grows with stage, radius stays small
    [DataField]
    public Color LightColor = Color.Red;

    [DataField]
    public float LightRadius = 1.2f;

    [DataField]
    public float LightEnergy = 0.5f;

    // runtime eye-burn tracking
    [ViewVariables]
    public int ShotsFired;

    [ViewVariables]
    public int AppliedEyeDamage;

    /// <summary>This tracks pointlight was added by virus or existed already, we dont strip if so.</summary>
    [ViewVariables]
    public bool AddedLight;
}

public sealed partial class LaserEyesActionEvent : WorldTargetActionEvent;
