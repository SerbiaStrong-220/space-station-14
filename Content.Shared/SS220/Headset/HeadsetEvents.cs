using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Headset;

[Serializable, NetSerializable]
public enum HeadsetKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HeadsetChannelToggledMessage : BoundUserInterfaceMessage
{
    public string ChannelKey;
    public bool Enabled;

    public HeadsetChannelToggledMessage(string channelKey, bool enabled)
    {
        ChannelKey = channelKey;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetBoundInterfaceState : BoundUserInterfaceState
{
    public List<(string Key, Color Color, string Name, bool Enabled)> Channels { get; }

    public HeadsetBoundInterfaceState(List<(string Key, Color Color, string Name, bool Enabled)> channels)
    {
        Channels = channels;
    }
}

[Serializable, NetSerializable]
public sealed partial class HeadsetSetListEvent : EntityEventArgs
{
    public NetEntity Owner;
    public List<(string id, Color color, string name, bool enabled)> ChannelList;

    public HeadsetSetListEvent(NetEntity owner, List<(string id, Color color, string name, bool enabled)> channelList)
    {
        Owner = owner;
        ChannelList = channelList;
    }
}
