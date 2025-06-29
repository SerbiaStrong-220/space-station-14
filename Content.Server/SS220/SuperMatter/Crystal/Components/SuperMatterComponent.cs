// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;


namespace Content.Server.SS220.SuperMatterCrystal.Components;
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SuperMatterComponent : Component
{
    // SM constants
    public const float MaximumIntegrity = 100f;
    public const float MinimumIntegrity = 0.01f;
    public const float MinimumMatter = 280f;
    public const float MinimumInternalEnergy = 2120f;

    // State flags
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsDelaminate = false;


    // Accumulators
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Name;
    [ViewVariables(VVAccess.ReadOnly)]
    public int UpdatesBetweenBroadcast;
    [ViewVariables(VVAccess.ReadOnly)]
    public float PressureAccumulator;
    [ViewVariables(VVAccess.ReadOnly)]
    public float MatterDervAccumulator;
    [ViewVariables(VVAccess.ReadOnly)]
    public float InternalEnergyDervAccumulator;
    [ViewVariables(VVAccess.ReadOnly)]
    public float IntegrityDamageAccumulator = 0f;
    [ViewVariables(VVAccess.ReadOnly)]
    public float AccumulatedZapEnergy = 0f;
    [ViewVariables(VVAccess.ReadOnly)]
    public float AccumulatedRadiationEnergy = 0f;
    /// <summary>
    /// Admins, you can use this to stop delamination just make it like 100 or more.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedRegenerationDelamination = 0f;
    [ViewVariables(VVAccess.ReadOnly)]
    public float NextRegenerationThreshold = 0f;
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<Gas, float> AccumulatedGasesMoles = new();

    // TimeSpans
    /// <summary> Current Value set to 3.5f cause for Arcs where is no point in lesser </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan OutputEnergySourceUpdateDelay = TimeSpan.FromSeconds(3.5f);
    [AutoPausedField]
    public TimeSpan NextOutputEnergySourceUpdate = default!;
    [AutoPausedField]
    public TimeSpan NextDamageImplementTime = default!;
    [AutoPausedField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextDamageStationAnnouncement = default!;
    [AutoPausedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TimeOfDelamination = default!;

    // SM params
    /// <summary>
    /// This used to init roundstart value in dimension less units
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float InitMatter = 200f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Integrity
    {
        get => _integrity;
        set
        {
            _integrity = value switch
            {
                < MinimumIntegrity => MinimumIntegrity,
                > MaximumIntegrity => MaximumIntegrity,
                _ => value
            };
        }
    }
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Temperature
    {
        get => _temperature;
        set
        {
            DebugTools.Assert(float.IsFinite(value));
            _temperature = value switch
            {
                < Atmospherics.TCMB => Atmospherics.TCMB,
                > Atmospherics.Tmax => Atmospherics.Tmax,
                _ => value
            };
        }
    }
    [ViewVariables(VVAccess.ReadWrite)]
    public float Matter
    {
        get => _matter;
        set
        {
            DebugTools.Assert(float.IsFinite(value));
            _matter = value switch
            {
                < MinimumMatter => MinimumMatter,
                _ => value
            };
        }
    }
    [ViewVariables(VVAccess.ReadWrite)]
    public float InternalEnergy
    {
        get => _internalEnergy;
        set
        {
            DebugTools.Assert(float.IsFinite(value));
            _internalEnergy = value switch
            {
                < MinimumInternalEnergy => MinimumInternalEnergy,
                _ => value
            };
        }
    }

    private float _integrity = 100f;
    private float _temperature = Atmospherics.T20C;
    private float _matter;
    private float _internalEnergy;

    // ProtoId Sector
    [DataField]
    public EntProtoId ConsumeResultEntityPrototype = "Ash";
    /// <summary> For future realization </summary>
    [DataField]
    public EntProtoId ResonanceSpawnPrototype = "TeslaEnergyBall";
    [DataField]
    public EntProtoId SingularitySpawnPrototype = "Singularity";
    [DataField]
    public EntProtoId TeslaSpawnPrototype = "TeslaEnergyBall";
    [DataField]
    public string DelaminateAlertLevel = "yellow";
    [DataField]
    public string CrystalDestroyAlertLevel = "delta";
    [DataField]
    public List<string> UnchangeableAlertLevelList = ["delta", "gamma", "epsilon"];

    public string? PreviousAlertLevel;

    // Audio Sector
    [DataField(required: true)]
    public SoundCollectionSpecifier ConsumeSound;
    [DataField]
    public SoundSpecifier CalmSound = new SoundPathSpecifier("/Audio/SS220/Ambience/Supermatter/calm.ogg");
    [DataField]
    public SoundSpecifier DelamSound = new SoundPathSpecifier("/Audio/SS220/Ambience/Supermatter/delamming.ogg");
}
