// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.AntagRemoval.Notifications;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.SS220.AntagRemoval.Notifications;

[UsedImplicitly]
public sealed class RoleRemovalNoticeEui : BaseEui
{
    private readonly RoleRemovalNoticeWindow _window;

    public RoleRemovalNoticeEui()
    {
        _window = new();

        _window.AcceptButton.OnPressed += _ => _window.Close();
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is RoleRemovalNoticeEuiState notice)
            _window.SetRemovedRole(notice.RemovedRole);
    }

    public override void Closed()
    {
        _window.Close();
    }
}
