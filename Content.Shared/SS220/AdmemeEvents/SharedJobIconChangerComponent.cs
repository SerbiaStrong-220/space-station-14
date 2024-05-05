// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AdmemeEvents;

[Serializable, NetSerializable]
public enum JobIconChangerKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum EventRoleIconFilterGroup : byte
{
    None,
    IOT,
    NT,
    USSP
}

[Serializable, NetSerializable]
public sealed class JobIconChangerChangedMessage : BoundUserInterfaceMessage
{
    public string? JobIcon { get; }

    public JobIconChangerChangedMessage(string jobIcon)
    {
        JobIcon = jobIcon;
    }
}

[Serializable, NetSerializable]
public sealed class JobIconChangerBoundUserInterfaceState : BoundUserInterfaceState
{
    public EventRoleIconFilterGroup Filter { get; }

    public JobIconChangerBoundUserInterfaceState(EventRoleIconFilterGroup filter)
    {
        Filter = filter;
    }
}
