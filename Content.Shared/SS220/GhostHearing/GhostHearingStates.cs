using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.GhostHearing;

[Serializable, NetSerializable]
public enum GhostHearingKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class GhostHearingBoundUIState : BoundUserInterfaceState
{
    public List<(string Key, Color Color, string Name, bool Enabled)> Channels { get; }

    public GhostHearingBoundUIState(List<(string Key, Color Color, string Name, bool Enabled)> channels)
    {
        Channels = channels;
    }
}

[Serializable, NetSerializable]
public sealed partial class GhostHearingChannelToggledMessage : BoundUserInterfaceMessage
{
    public string ChannelKey;
    public bool Enabled;

    public GhostHearingChannelToggledMessage(string channelKey, bool enabled)
    {
        ChannelKey = channelKey;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed partial class GhostHearingToggledAllChannelsMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public GhostHearingToggledAllChannelsMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed partial class GhostHearingSetListEvent : EntityEventArgs
{
    public NetEntity Owner;
    public List<(string id, Color color, string name, bool enabled)> ChannelList;

    public GhostHearingSetListEvent(NetEntity owner, List<(string id, Color color, string name, bool enabled)> channelList)
    {
        Owner = owner;
        ChannelList = channelList;
    }
}

public sealed partial class ToggleGhostRadioChannels : InstantActionEvent { }
