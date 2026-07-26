// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class ManageStatusesEuiState : EuiStateBase
{
    public string TargetName { get; }
    public List<string> ActiveStatusIds { get; }
    public Dictionary<string, string> AllStatuses { get; }

    public ManageStatusesEuiState(string targetName, List<string> activeStatusIds, Dictionary<string, string> allStatuses)
    {
        TargetName = targetName;
        ActiveStatusIds = activeStatusIds;
        AllStatuses = allStatuses;
    }
}

[Serializable, NetSerializable]
public sealed class AddStatusMessage : EuiMessageBase
{
    public string StatusId { get; }
    public AddStatusMessage(string statusId) { StatusId = statusId; }
}

[Serializable, NetSerializable]
public sealed class RemoveStatusMessage : EuiMessageBase
{
    public string StatusId { get; }
    public RemoveStatusMessage(string statusId) { StatusId = statusId; }
}
