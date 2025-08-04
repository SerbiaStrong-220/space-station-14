// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.SuperMatter.Emitter;

public sealed class SuperMatterEmitterExtensionConsts
{
    public const int BaseEnergyConsumption = 600;
    private const float BaseMatter = 15f;
    private const float BaseMatterPowerDivider = 300f;
    public static float GetMatterFromPower(float power)
    {
        return BaseMatter * MathF.Sqrt(power / BaseMatterPowerDivider);
    }
    private const float BaseEnergy = 90f;
    private const float BaseEnergyPowerDivider = 300f;
    public static float GetEnergyFromPower(float power)
    {
        return BaseEnergy * MathF.Sqrt(power / BaseEnergyPowerDivider);
    }
}
