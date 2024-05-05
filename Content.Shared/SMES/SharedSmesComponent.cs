using Robust.Shared.Serialization;

namespace Content.Shared.SMES;

[Serializable, NetSerializable]
public enum SmesVisuals
{
    LastChargeState,
    LastChargeLevel,
}

// SS220 smes-ui-fix begin
[Serializable, NetSerializable]
public enum SmesUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SmesState : BoundUserInterfaceState
{
    public string EntityName { get; }
    public string DeviceNetworkAddress { get; }
    public int BatteryCurrentCharge { get; }
    public int BatteryMaxCharge { get; }
    public int BatteryChargePercentRounded { get; }

    public SmesState(string entityName,
        string deviceNetworkAddress,
        int batteryCurrentCharge,
        int batteryMaxCharge,
        int batteryChargePercentRounded)
    {
        EntityName = entityName;
        DeviceNetworkAddress = deviceNetworkAddress;
        BatteryCurrentCharge = batteryCurrentCharge;
        BatteryMaxCharge = batteryMaxCharge;
        BatteryChargePercentRounded = batteryChargePercentRounded;
    }
}
// SS220 smes-ui-fix end
