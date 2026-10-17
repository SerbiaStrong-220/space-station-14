// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiThermalOverrideComponent : Component;

[RegisterComponent]
public sealed partial class MalfAiAirFloodComponent : Component;

[RegisterComponent]
public sealed partial class MalfAiCameraUpgradeComponent : Component
{
    [DataField]
    public float VisionRadius = 12f;

    [DataField]
    public float CameraRange = MalfAiConstants.CameraRange;

    [DataField]
    public float CloseRadius = 4f;
}

[RegisterComponent]
public sealed partial class MalfAiBlackoutTuningComponent : Component
{
    [DataField]
    public float IgniteTemp = 700f;

    [DataField]
    public float IgniteVolume = 50f;

    [DataField]
    public float EmpRange = 6f;

    [DataField]
    public float EmpEnergy = 25000f;

    [DataField]
    public float EmpDuration = 15f;
}

[RegisterComponent]
public sealed partial class MalfAiMachineOverrideTuningComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    [DataField]
    public int Damage = 10;

    [DataField]
    public float Range = 1.2f;
}

[RegisterComponent]
public sealed partial class MalfAiMachineOverloadTuningComponent : Component
{
    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionType = "Minibomb";

    [DataField]
    public float TotalIntensity = 30f;

    [DataField]
    public float IntensitySlope = 20f;

    [DataField]
    public float MaxIntensity = 20f;

    [DataField]
    public float DelaySeconds = 3f;

    [DataField]
    public float BeepIntervalSeconds = 1f;

    [DataField]
    public SoundSpecifier? BeepSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg")
    {
        Params = AudioParams.Default.WithVolume(-4f),
    };
}

[RegisterComponent]
public sealed partial class MalfAiChaosPulseComponent : Component
{
    [DataField]
    public List<MalfAiChaosEventEntry> Events = new();
}

[DataDefinition]
public sealed partial class MalfAiChaosEventEntry
{
    [DataField(required: true)]
    public EntProtoId Event;

    [DataField]
    public float Weight = 1f;
}
