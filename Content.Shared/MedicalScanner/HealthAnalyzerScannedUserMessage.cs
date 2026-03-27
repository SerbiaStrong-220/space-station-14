using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool CanPrint; // SS220-health-analyzer-report
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public int? CounterDeath; //SS220 LimitationRevive

    //SS220 LimitationRevive - start
    public HealthAnalyzerScannedUserMessage(NetEntity? targetEntity, float temperature, float bloodLevel, bool canPrint, bool? scanMode, bool? bleeding, bool? unrevivable, int? counterDeath) // SS220-health-analyzer-report
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        CanPrint = canPrint; // SS220-health-analyzer-report
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        CounterDeath = counterDeath;
    }
    //SS220 LimitationRevive - end
}

