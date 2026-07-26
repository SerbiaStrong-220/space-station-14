// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Client.Eui;
using JetBrains.Annotations;

namespace Content.Client.Administration.UI.ManageStatuses;

[UsedImplicitly]
public sealed class ManageStatusesEui : BaseEui
{
    private readonly ManageStatusesWindow _window;

    public ManageStatusesEui()
    {
        _window = new ManageStatusesWindow();
        _window.OnAddStatus += statusId => SendMessage(new AddStatusMessage(statusId));
        _window.OnRemoveStatus += statusId => SendMessage(new RemoveStatusMessage(statusId));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Close();

    public override void HandleState(EuiStateBase state)
    {
        if (state is ManageStatusesEuiState s)
            _window.UpdateState(s.TargetName, s.ActiveStatusIds, s.AllStatuses);
    }
}
