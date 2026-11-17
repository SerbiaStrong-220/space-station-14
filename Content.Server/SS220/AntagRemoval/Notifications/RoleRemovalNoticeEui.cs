// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.AntagRemoval.Notifications;

namespace Content.Server.SS220.AntagRemoval.Notifications;

public sealed class RoleRemovalNoticeEui : BaseEui
{
    private readonly string? _removedRole;

    public RoleRemovalNoticeEui(string? removedRole = null)
    {
        _removedRole = removedRole;
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new RoleRemovalNoticeEuiState(_removedRole);
    }
}
