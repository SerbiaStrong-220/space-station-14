// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.MindSlave;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.SS220.MindSlave.UI;

//Stolen from DeathReminder
[UsedImplicitly]
public sealed class MindSlaveNotificationEui : BaseEui
{
    private readonly MindSlaveNotificationWindow _window;

    public MindSlaveNotificationEui()
    {
        _window = new MindSlaveNotificationWindow();

        _window.AcceptButton.OnPressed += _ =>
        {
            _window.Close();
        };
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not MindSlaveNotificationEuiState newState)
            return;

        _window.TextLabel.Text = newState.IsEnslaved ?
            Loc.GetString("mindslave-notification-window-text-enslaved", ("name", newState.MasterName)) :
            Loc.GetString("mindslave-notification-window-text-freed");
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }
}

