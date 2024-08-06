// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Server.Construction.Completions;
using Content.Server.Chat.Commands;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    /* TODOs:
        [ ] Make Internal log of starting SM
        [ ] Think of "Helps us prevent cases when someone dumps superhothotgas into the SM and shoots the power to the moon for one tick." (c) TG

    */

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    private float GetSingularityTeslaEquilibriumPressure(float temperature) => SuperMatterPhaseDiagram.GetSingularityTeslaEquilibriumPressure(temperature);
    private float GetSingularityResonanceEquilibriumPressure(float temperature) => SuperMatterPhaseDiagram.GetSingularityResonanceEquilibriumPressure(temperature);
    private float GetResonanceTeslaEquilibriumPressure(float temperature) => SuperMatterPhaseDiagram.GetResonanceTeslaEquilibriumPressure(temperature);
    private float GetDecayMatterMultiplier(float temperature, float pressure) => SuperMatterInternalProcess.GetDecayMatterMultiplier(temperature, pressure);
    private float GetMolesReactionEfficiency(float temperature, float pressure) => SuperMatterInternalProcess.GetMolesReactionEfficiency(temperature, pressure);
    private float GetDeltaChemistryPotential(float temperature, float pressure) => SuperMatterInternalProcess.GetDeltaChemistryPotential(temperature, pressure);
    private float GetHeatCapacity(float temperature, float matter) => SuperMatterInternalProcess.GetHeatCapacity(temperature, matter);
    private float GetRelativeGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture) => SuperMatterGasResponse.GetRelativeGasesInfluenceToMatterDecay(smComp, gasMixture);
    private float GetFlatGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture) => SuperMatterGasResponse.GetFlatGasesInfluenceToMatterDecay(smComp, gasMixture);

    public const float MatterNondimensionalization = 12f; // like C mass in Mendeleev table
    public const float CHEMISTRY_POTENTIAL_BASE = 10f; // parrots now, but need to concrete in future
    private const float MATTER_DECAY_BASE_RATE = 80f; // parrots now, but need to concrete in future
    /// <summary> Defines how fast SM gets in thermal equilibrium with gas in it. Do not make it greater than 1! </summary>
    private const float SM_HEAT_TRANSFER_RATIO = 0.07f;

    public void EvaluateDeltaInternalEnergy(Entity<SuperMatterComponent> crystal, GasMixture gasMixture, float frameTime)
    {
        var (crystalUid, smComp) = crystal;
        var chemistryPotential = CHEMISTRY_POTENTIAL_BASE + GetDeltaChemistryPotential(smComp.Temperature, gasMixture.Pressure);
        var crystalHeatFromGas = _atmosphere.GetThermalEnergy(gasMixture) * SM_HEAT_TRANSFER_RATIO
                                    * gasMixture.Temperature - smComp.Temperature
                                    / MathF.Max(gasMixture.Temperature, smComp.Temperature);
        var smDeltaT = crystalHeatFromGas / GetHeatCapacity(smComp.Temperature, smComp.Matter);
        var normalizedMatter = smComp.Matter / MatterNondimensionalization;
        // here we start to change mix in SM cause nothing else depends on it after
        var deltaMatter = SynthesizeMatterFromGas(crystal, gasMixture, frameTime) - CalculateDecayedMatter(crystal, gasMixture);
        var normalizedDeltaMatter = deltaMatter / MatterNondimensionalization;

        var matterToTemperatureRatio = normalizedMatter / smComp.Temperature;
        var newMatterToTemperatureRatio = (normalizedMatter + normalizedDeltaMatter) / (smComp.Temperature + smDeltaT);
        // here we connect chemistry potential with internal energy, so thought of their units adequate, maybe even calculate it
        var deltaInternalEnergy = (smDeltaT * (matterToTemperatureRatio - newMatterToTemperatureRatio) * smComp.InternalEnergy
                                    + chemistryPotential * normalizedDeltaMatter)
                                    / (1 + newMatterToTemperatureRatio * smDeltaT);

        smComp.InternalEnergy += deltaInternalEnergy * frameTime;
        if (smComp.InternalEnergy < 0)
        {
            // TODO loc it
            Log.Error($"Internal Energy of SuperMatter {crystal} became negative, forced to truthish value.");
            _chatManager.SendAdminAlert($"SuperMatter {crystal} caught physics law breaking! If it possible ask how they do it and convey it to developer");
            smComp.InternalEnergy = EvaluateTruthishInternalEnergy(crystal);
        }
        smComp.Matter = MathF.Max(smComp.Matter + deltaMatter * frameTime, MatterNondimensionalization); // actually should go boom at this low, but...
        smComp.Temperature = MathF.Max(MathF.Min(Atmospherics.Tmax, smComp.Temperature + smDeltaT * frameTime), Atmospherics.TCMB); // weird but okay
        _atmosphere.AddHeat(gasMixture, -crystalHeatFromGas * frameTime);
    }
    /// <summary> We dont apply it to Matter field of SMComp because we need this value in internal energy evaluation </summary>
    private float CalculateDecayedMatter(Entity<SuperMatterComponent> crystal, GasMixture gasMixture)
    {
        var (crystalUid, smComp) = crystal;
        var gasEffectMultiplier = GetRelativeGasesInfluenceToMatterDecay(smComp, gasMixture);
        var gasFlatInfluence = GetFlatGasesInfluenceToMatterDecay(smComp, gasMixture);

        /// gas effect multiplier should affects only Base decay rate, f.e. for gases which mostly occupy SM decay
        var environmentMultiplier = GetDecayMatterMultiplier(smComp.Temperature, gasMixture.Pressure);
        environmentMultiplier = Math.Max(environmentMultiplier, 20f); // cut off enormous numbers, our goal fun not overwhelm

        return (MATTER_DECAY_BASE_RATE * gasEffectMultiplier + gasFlatInfluence) * environmentMultiplier;
    }
    /// <summary> Calculate how much matter will be added this step
    ///  and distract used gas from its inner gasMixture if deleteUsedGases true
    /// We dont apply it to Matter field of SMComp because we need this value in internal energy evaluation </summary>
    private float SynthesizeMatterFromGas(Entity<SuperMatterComponent> crystal, GasMixture gasMixture, float frameTime, bool deleteUsedGases = true)
    {
        var (crystalUid, smComp) = crystal;
        var resultAdditionalMatter = 0f;

        foreach (var gasId in smComp.GasesToMatterConvertRatio.Keys)
        {
            var gasMolesInReact = gasMixture.GetMoles(gasId)
                                    * GetMolesReactionEfficiency(smComp.Temperature, gasMixture.Pressure) ;

            if (deleteUsedGases)
                gasMixture.AdjustMoles(gasId, gasMolesInReact * frameTime);
            resultAdditionalMatter += gasMolesInReact * smComp.GasesToMatterConvertRatio[gasId];
        }

        return resultAdditionalMatter;
    }
    private SuperMatterPhaseState GetSuperMatterPhase(Entity<SuperMatterComponent> crystal, GasMixture gasMixture)
    {
        var (crystalUid, smComp) = crystal;

        if (smComp.Temperature < Atmospherics.T20C)
        {
            if (gasMixture.Pressure < GetSingularityTeslaEquilibriumPressure(smComp.Temperature))
                return SuperMatterPhaseState.TeslaRegion;
            else
                return SuperMatterPhaseState.SingularityRegion;
        }
        if (smComp.Temperature > Atmospherics.T20C)
        {
            if (gasMixture.Pressure < GetResonanceTeslaEquilibriumPressure(smComp.Temperature))
                return SuperMatterPhaseState.TeslaRegion;
            if (gasMixture.Pressure < GetSingularityResonanceEquilibriumPressure(smComp.Temperature))
                return SuperMatterPhaseState.ResonanceRegion;
            return SuperMatterPhaseState.TeslaRegion;
        }

        return SuperMatterPhaseState.InertRegion;
    }
    /// <summary>
    /// In future maybe useful if a need to make/init own SM gasStructs
    /// </summary>
    private bool TryGetCrystalGasMixture(EntityUid crystalUid, [NotNullWhen(true)] out GasMixture? gasMixture)
    {
        gasMixture = _atmosphere.GetContainingMixture(crystalUid, true, true);
        if (gasMixture == null)
            return false;
        return true;
    }

    private float EvaluateTruthishInternalEnergy(Entity<SuperMatterComponent> crystal)
    {
        var (_, smComp) = crystal;
        return smComp.Matter * (CHEMISTRY_POTENTIAL_BASE) + GetHeatCapacity(smComp.Temperature, smComp.Matter) * smComp.Temperature;
    }

}
