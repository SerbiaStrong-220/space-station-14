using Content.Shared.FixedPoint;
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
public sealed class HeadsetChangeFrequencyMessage : BoundUserInterfaceMessage
{
    public FixedPoint2 Frequency;

    public HeadsetChangeFrequencyMessage(FixedPoint2 frequency)
    {
        Frequency = frequency;
    }
}


[Serializable, NetSerializable]
public sealed class HeadsetBoundInterfaceState : BoundUserInterfaceState
{
    public List<(string Key, Color Color, string Name, bool Enabled)> Channels { get; }

    public (FixedPoint2 minValue, FixedPoint2 maxValue, FixedPoint2 value)? RadioFrequencySettings { get; init; }

    public HeadsetBoundInterfaceState(List<(string Key, Color Color, string Name, bool Enabled)> channels, (FixedPoint2 minValue, FixedPoint2 maxValue, FixedPoint2 value)? radioFrequencySettings = null)
    {
        Channels = channels;
        RadioFrequencySettings = radioFrequencySettings;
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
