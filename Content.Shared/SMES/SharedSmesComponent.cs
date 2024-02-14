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
    public int BatteryChargePercentRounded { get; }


    public SmesState(string entityName, string deviceNetworkAddress, int batteryChargePercentRounded)
    {
        EntityName = entityName;
        DeviceNetworkAddress = deviceNetworkAddress;
        BatteryChargePercentRounded = batteryChargePercentRounded;
    }
}
// SS220 smes-ui-fix end
