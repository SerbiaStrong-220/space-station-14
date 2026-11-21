// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AntagRemoval.Notifications;

/// <summary>Role name is null for ordinary cult deconversion.</summary>
[Serializable, NetSerializable]
public sealed class RoleRemovalNoticeEuiState : EuiStateBase
{
    public readonly string? RemovedRole;

    public RoleRemovalNoticeEuiState(string? removedRole)
    {
        RemovedRole = removedRole;
    }
}
