// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CrewManifest;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class ManageDeliveryEuiState : EuiStateBase
{
    public string TargetName = string.Empty;
    public int BaseSpesoReward;
    public bool HasTimer;
    public int TimeLeftSeconds;
    public string? RecipientName;
    public string? RecipientJobTitle;
    public List<(uint, CrewManifestEntry)> Crew = [];
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

