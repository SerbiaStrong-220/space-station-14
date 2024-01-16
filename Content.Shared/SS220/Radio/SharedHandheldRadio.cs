// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Radio;

[Serializable, NetSerializable]
public enum HandheldRadioUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HandheldRadioBoundUIState : BoundUserInterfaceState
{
    public bool MicEnabled;
    public bool SpeakerEnabled;
    public List<string> AvailableChannels;
    public string SelectedChannel;

    public HandheldRadioBoundUIState(bool micEnabled, bool speakerEnabled, List<string> availableChannels, string selectedChannel)
    {
        MicEnabled = micEnabled;
        SpeakerEnabled = speakerEnabled;
        AvailableChannels = availableChannels;
        SelectedChannel = selectedChannel;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleHandheldRadioMicMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleHandheldRadioMicMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class ToggleHandheldRadioSpeakerMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public ToggleHandheldRadioSpeakerMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class SelectHandheldRadioChannelMessage : BoundUserInterfaceMessage
{
    public string Channel;

    public SelectHandheldRadioChannelMessage(string channel)
    {
        Channel = channel;
    }
}