using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public HealthAnalyzerUiState State;

    public HealthAnalyzerScannedUserMessage(HealthAnalyzerUiState state)
    {
        State = state;
    }
}

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public struct HealthAnalyzerUiState
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool CanPrint; // SS220-health-analyzer-report
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public int? CounterDeath; //SS220 LimitationRevive

    public HealthAnalyzerUiState() {}

    public HealthAnalyzerUiState(NetEntity? targetEntity, float temperature, float bloodLevel, bool? scanMode, bool? bleeding, bool? unrevivable, int? counterDeath /* SS220 LimitationRevive */, bool canPrint /*  SS220-health-analyzer-report */)
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        CanPrint = canPrint; // SS220-health-analyzer-report
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        CounterDeath = counterDeath; // SS220 LimitationRevive
    }
}
