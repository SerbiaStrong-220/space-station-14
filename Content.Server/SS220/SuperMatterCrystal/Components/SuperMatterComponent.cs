// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal;
using Content.Shared.Atmos;


namespace Content.Server.SS220.SuperMatterCrystal.Components;
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class SuperMatterComponent : Component
{
    /// <summary> The SM will only cycle if activated. </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DisabledByAdmin = false;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float AccumulatedZapEnergy = 0f;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float AccumulatedRadiationEnergy = 0f;

    /// <summary> Current Value set to 4.1f cause for Arcs where is no point in lesser </summary>
    [DataField]
    public TimeSpan OutputEnergySourceUpdateDelay = TimeSpan.FromSeconds(4.1f);
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan NextOutputEnergySourceUpdate = default!;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Temperature = Atmospherics.T20C;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Matter = 200 * SuperMatterSystem.MatterNondimensionalization; // To wrap it in own VAR
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float InternalEnergy = 2e4f; // If wrong parrots we will log it on Comp init in System and force none error value

    #region GasInteraction
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<Gas, (float RelativeInfluence, float flatInfluence)> DecayInfluenceGases;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    // It is used to define how much matter will be added if 1 mole of gas consumed
    public Dictionary<Gas, float> GasesToMatterConvertRatio;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<Gas, (float OptimalRatio, float RelativeInfluence)> EnergyEfficiencyChangerGases;

    #endregion
}

public enum SuperMatterPhaseState
{
    ErrorState = -1,
    InertRegion,
    SingularityRegion,
    ResonanceRegion,
    TeslaRegion
}
