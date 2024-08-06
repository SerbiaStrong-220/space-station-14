// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal;
using Content.Shared.Atmos;


namespace Content.Server.SS220.SuperMatterCrystal.Components;
[RegisterComponent]
public sealed partial class SuperMatterComponent : Component
{
    /// <summary> The SM will only cycle if activated. </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DisabledByAdmin = false;
    [DataField("nextUpdate")]
    public TimeSpan NextUpdate = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Temperature = Atmospherics.T20C;
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Matter = 1000 * SuperMatterSystem.MatterNondimensionalization; // To wrap it in own VAR
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float InternalEnergy = 1e7f; // If wrong parrots we will log it on Comp init in System and force none error value

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
