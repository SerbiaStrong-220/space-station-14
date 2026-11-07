// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SiliconComponents;

[Serializable, NetSerializable]
public enum SiliconUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SiliconEjectPartBuiMessage : BoundUserInterfaceMessage
{
    public PartType DesiredPart;

    public SiliconEjectPartBuiMessage(PartType desiredPart)
    {
        DesiredPart = desiredPart;
    }
}

[Serializable, NetSerializable]
public sealed class SiliconEjectBatteryBuiMessage : BoundUserInterfaceMessage;


[Serializable, NetSerializable]
public sealed class SiliconRemoveModuleBuiMessage(NetEntity module) : BoundUserInterfaceMessage
{
    public NetEntity Module = module;
}
