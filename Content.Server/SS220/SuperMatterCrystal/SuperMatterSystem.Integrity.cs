// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;
using Content.Server.SS220.SuperMatterCrystal.Components;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    /// <summary> Based lie, negative damage = heal, no exception will thrown </summary>
    /// <returns> if positive - damage, if negative - heal </returns>
    public float GetIntegrityDamage(SuperMatterComponent smComp)
    {
        var (internalEnergy, matter, temperature) = (smComp.InternalEnergy, smComp.Matter, smComp.Temperature);
        var damageFromDelta = GetInternalEnergyToMatterDamageFactor(internalEnergy, matter);
        var temperatureFactor = TemperatureDamageFactorFunction(temperature);
        var damage = damageFromDelta > 0 ? damageFromDelta * temperatureFactor : damageFromDelta;
        return damage;
    }
    public float GetInternalEnergyToMatterDamageFactor(float internalEnergy, float matter)
    {
        var safeInternalEnergy = GetSafeInternalEnergyToMatterValue(matter);
        var delta = internalEnergy - safeInternalEnergy;
        var damageFromDelta = EnergyToMatterDamageFactorFunction(delta);
        return damageFromDelta;
    }
    public float GetSafeInternalEnergyToMatterValue(float matter)
    {
        var normalizedMatter = matter / MatterNondimensionalization;
        return SafeInternalEnergyToMatterFunction(normalizedMatter);
    }
    public void AddIntegrityDamage(SuperMatterComponent smComp, float damage)
    {
        smComp.IntegrityDamageAccumulator += damage;
    }
    public float GetIntegrity(SuperMatterComponent smComp)
    {
        return MathF.Round(smComp.Integrity, 2);
    }

    private const float MaxDamagePerSecond = 1.5f;
    private const float MaxRegenerationPerSecond = 1.5f;
    /// <summary> Based lie, negative damage = heal, no exception will thrown </summary>
    /// <returns> Return false only if SM integrity WILL fall below zero, but wont set it to zero </returns>
    private bool TryImplementIntegrityDamage(SuperMatterComponent smComp)
    {
        var resultIntegrityDamage = Math.Clamp(smComp.IntegrityDamageAccumulator, -MaxDamagePerSecond, MaxRegenerationPerSecond);
        if (smComp.Integrity + resultIntegrityDamage < 0f)
            return false;
        if (smComp.Integrity + resultIntegrityDamage < 100f)
        {
            smComp.Integrity += resultIntegrityDamage;
            return true;
        }
        if (smComp.Integrity != 100f)
            smComp.Integrity = 100f;
        return true;
    }

    private const float EnergyToMatterDamageFactorSlowerOffset = 1000f;
    private const float EnergyToMatterDamageFactorDeltaOffset = 1000f;
    private float EnergyToMatterDamageFactorFunction(float delta)
    {
        var offsetedDelta = delta - EnergyToMatterDamageFactorDeltaOffset;
        return offsetedDelta / (MathF.Abs(offsetedDelta) + EnergyToMatterDamageFactorSlowerOffset);
    }

    private const float SafeInternalEnergyToMatterCoeff = 800f;
    private const float SafeInternalEnergyToMatterSlowerOffset = 50f;
    private float SafeInternalEnergyToMatterFunction(float normalizedMatter)
    {
        return SafeInternalEnergyToMatterCoeff * MathF.Pow(normalizedMatter, 1.5f)
                            / (normalizedMatter + SafeInternalEnergyToMatterSlowerOffset);
    }

    private const float TemperatureDamageFactorCoeff = 3f;
    private const float TemperatureDamageFactorSlowerOffset = 20f;
    private float TemperatureDamageFactorFunction(float normalizedTemperature)
    {
        var normalizedMaxTemperature = Atmospherics.Tmax / SuperMatterPhaseDiagram.SuperMatterTriplePointTemperature;
        var maxFuncValue = MathF.Pow(normalizedMaxTemperature, 1.5f) /
                (normalizedMaxTemperature - TemperatureDamageFactorSlowerOffset);

        return TemperatureDamageFactorCoeff * (MathF.Pow(normalizedTemperature, 1.5f) /
                (normalizedTemperature - TemperatureDamageFactorSlowerOffset)) / maxFuncValue;
    }

}
