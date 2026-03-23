// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Trigger;

[Serializable, NetSerializable]
public sealed class RattleBoundUiState : BoundUserInterfaceState
{
    public List<RattleChannelEntry> Channels { get; }

    public RattleBoundUiState(List<RattleChannelEntry> channels)
    {
        Channels = channels;
    }
}

[Serializable, NetSerializable]
public readonly record struct RattleChannelEntry(string Key, Color Color, string Name, bool Enabled);

[Serializable, NetSerializable]
public sealed class RattleChannelToggledMessage : BoundUserInterfaceMessage
{
    public string ChannelKey;
    public bool Enabled;

    public RattleChannelToggledMessage(string channelKey, bool enabled)
    {
        ChannelKey = channelKey;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class RattleToggleAllChannelsMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public RattleToggleAllChannelsMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public enum RattleUIKey
{
    Key,
}

