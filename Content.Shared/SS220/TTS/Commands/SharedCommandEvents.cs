// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS.Commands;

[Serializable, NetSerializable]
public sealed class TtsQueueResetMessage : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class SessionSendTTSMessage(bool value) : EntityEventArgs
{
    public bool Value { get; init; } = value;
}
