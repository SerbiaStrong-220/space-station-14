// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MalfAI;

public static class MalfAiConstants
{
    public const string ZeroLawMarker = "MalfZero";

    public static readonly ProtoId<CurrencyPrototype> CpuCurrency = "MalfCPU";
    public static readonly ProtoId<ListingPrototype> BlackoutListing = "MalfAiBlackout";
    public static readonly ProtoId<ListingPrototype> ThermalListing = "MalfAiThermalOverride";
    public static readonly ProtoId<ListingPrototype> FloodListing = "MalfAiAirFlood";
    public static readonly ProtoId<ListingPrototype> LockdownListing = "MalfAiLockdown";
    public static readonly ProtoId<ListingPrototype> TurretUpgradeListing = "MalfAiTurretUpgrade";
    public static readonly ProtoId<ListingPrototype> DeployTurretListing = "MalfAiDeployTurret";
    public static readonly ProtoId<ListingPrototype> MachineOverrideListing = "MalfAiMachineOverride";
    public static readonly ProtoId<ListingPrototype> MachineOverloadListing = "MalfAiMachineOverload";
    public static readonly ProtoId<ListingPrototype> RobotFactoryListing = "MalfAiRobotFactory";
    public static readonly ProtoId<ListingPrototype> DoomsdayListing = "MalfAiDoomsday";
    public static readonly ProtoId<ListingPrototype> SilentRecordsListing = "MalfAiSilentRecords";
    public static readonly ProtoId<ListingPrototype> PowerSiphonListing = "MalfAiPowerSiphon";
    public static readonly ProtoId<ListingPrototype> CameraUpgradeListing = "MalfAiCameraUpgrade";
    public static readonly ProtoId<ListingPrototype> ChaosPulseListing = "MalfAiChaosPulse";
    public static readonly ProtoId<ListingPrototype> EmagListing = "MalfAiEmag";

    public const float CameraRange = 12f;
}
