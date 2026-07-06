// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VirusLaserEyesComponent : Component
{
    /// <summary>Proto of projectile(damage is overridden).</summary>
    [DataField]
    public EntProtoId LaserProto = "BulletLaser";

    /// <summary>Speed of projectile.</summary>
    [DataField]
    public float LaserSpeed = 20f;

    /// <summary>How far in front of caster projectile spawns, so it doesn't clip.</summary>
    [DataField]
    public float SpawnOffset = 1f;

    /// <summary>Minimum aim distance.</summary>
    [DataField]
    public float AimDeadzoneSq = 0.01f;

    /// <summary>Projectile damage for current stage.</summary>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>Shots needed to reach full eye damage.</summary>
    [DataField]
    public int ShotsToMax = 18;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/laser.ogg");

    [DataField]
    public EntProtoId ActionId = "VirusActionLaserEyes";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public Color LightColor = Color.Red;

    [DataField]
    public float LightRadius = 1.2f;

    [DataField]
    public float LightEnergy = 0.5f;

    [ViewVariables]
    public int ShotsFired;

    [ViewVariables]
    public int AppliedEyeDamage;

    /// <summary>Data of pointlight, so cure restores the carrier's own light.</summary>
    [ViewVariables]
    public VirusGlowState Glow;
}

public sealed partial class VirusLaserEyesActionEvent : WorldTargetActionEvent;
