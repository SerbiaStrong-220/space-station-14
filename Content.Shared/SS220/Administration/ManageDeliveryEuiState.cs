// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CrewManifest;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration;

[Serializable, NetSerializable]
public sealed class ManageDeliveryEuiState(
    string targetName,
    int baseSpesoReward = 0,
    bool hasTimer = false,
    int timeLeftSeconds = 0,
    string? recipientName = null,
    string? recipientJobTitle = null,
    string? contentsProtoId = null,
    List<(uint, CrewManifestEntry)>? crew = null) : EuiStateBase
{
    public string TargetName { get; } = targetName;
    public int BaseSpesoReward { get; } = baseSpesoReward;
    public bool HasTimer { get; } = hasTimer;
    public int TimeLeftSeconds { get; } = timeLeftSeconds;
    public string? RecipientName { get; } = recipientName;
    public string? RecipientJobTitle { get; } = recipientJobTitle;
    public string? ContentsProtoId { get; } = contentsProtoId;
    public List<(uint, CrewManifestEntry)> Crew { get; } = crew ?? [];
}

[Serializable, NetSerializable]
public sealed class SetDeliveryRewardMessage(int reward) : EuiMessageBase
{
    public int Reward { get; } = reward;
}

[Serializable, NetSerializable]
public sealed class SetDeliveryRecipientMessage(uint? recordId) : EuiMessageBase
{
    public uint? RecordId { get; } = recordId;
}

[Serializable, NetSerializable]
public sealed class SetDeliveryTimerMessage(int seconds) : EuiMessageBase
{
    public int Seconds { get; } = seconds;
}

[Serializable, NetSerializable]
public sealed class ReplaceDeliveryContentsMessage(string protoId) : EuiMessageBase
{
    public string ProtoId { get; } = protoId;
}

[Serializable, NetSerializable]
public sealed class ClearDeliveryContentsMessage : EuiMessageBase;

