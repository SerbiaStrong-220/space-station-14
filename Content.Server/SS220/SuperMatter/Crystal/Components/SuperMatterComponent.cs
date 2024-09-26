// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.SuperMatterCrystal.Components;
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SuperMatterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;
    /// <summary> Super flag for freezing all SM interaction.
    /// Only changing it to true will invoke base SM logic </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DisabledByAdmin = false;
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
    [ViewVariables(VVAccess.ReadOnly)]
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
    [AutoPausedField]
    public TimeSpan NextDamageStationAnnouncement = default!;
    [AutoPausedField]
    public TimeSpan TimeOfDelamination = default!;


    // SM params

    [ViewVariables(VVAccess.ReadOnly)]
    public float Integrity = 100f;
    [ViewVariables(VVAccess.ReadOnly)]
    public float Temperature = Atmospherics.T20C;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Matter = 200 * SuperMatterSystem.MatterNondimensionalization; // To wrap it in own VAR
    /// <summary> Will be set in CompInit by system</summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float InternalEnergy = 0f;

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

    public string? PreviousAlertLevel;

    // Audio Sector

    [DataField(required: true)]
    public SoundCollectionSpecifier ConsumeSound;
    [DataField]
    public SoundSpecifier CalmSound = new SoundPathSpecifier("/Audio/SS220/Ambience/Supermatter/calm.ogg");
    [DataField]
    public SoundSpecifier DelamSound = new SoundPathSpecifier("/Audio/SS220/Ambience/Supermatter/delamming.ogg");
}
