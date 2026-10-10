// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CrewManifest;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration;

[Serializable, NetSerializable]
public sealed class ManageDeliveryEuiState : EuiStateBase
{
    public string TargetName { get; }
    public int BaseSpesoReward { get; }
    public bool HasTimer { get; }
    public int TimeLeftSeconds { get; }
    public string? RecipientName { get; }
    public string? RecipientJobTitle { get; }
    public string? ContentsProtoId { get; }
    public List<(uint, CrewManifestEntry)> Crew { get; }

    public ManageDeliveryEuiState(
        string targetName,
        int baseSpesoReward,
        bool hasTimer,
        int timeLeftSeconds,
        string? recipientName,
        string? recipientJobTitle,
        string? contentsProtoId,
        List<(uint, CrewManifestEntry)> crew)
    {
        TargetName = targetName;
        BaseSpesoReward = baseSpesoReward;
        HasTimer = hasTimer;
        TimeLeftSeconds = timeLeftSeconds;
        RecipientName = recipientName;
        RecipientJobTitle = recipientJobTitle;
        ContentsProtoId = contentsProtoId;
        Crew = crew;
    }
}

[Serializable, NetSerializable]
public sealed class SetDeliveryRewardMessage : EuiMessageBase
{
    public int Reward { get; }

    public SetDeliveryRewardMessage(int reward)
    {
        Reward = reward;
    }
}

[Serializable, NetSerializable]
public sealed class SetDeliveryRecipientMessage : EuiMessageBase
{
    public uint? RecordId { get; }

    public SetDeliveryRecipientMessage(uint? recordId)
    {
        RecordId = recordId;
    }
}

[Serializable, NetSerializable]
public sealed class SetDeliveryTimerMessage : EuiMessageBase
{
    public int Seconds { get; }

    public SetDeliveryTimerMessage(int seconds)
    {
        Seconds = seconds;
    }
}

[Serializable, NetSerializable]
public sealed class ReplaceDeliveryContentsMessage : EuiMessageBase
{
    public string ProtoId { get; }

    public ReplaceDeliveryContentsMessage(string protoId)
    {
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public sealed class ClearDeliveryContentsMessage : EuiMessageBase;

