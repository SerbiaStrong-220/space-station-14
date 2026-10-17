using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.Administration.Connections;
using JetBrains.Annotations;
using static Content.Shared.SS220.Administration.Connections.ConnectionLogsEuiMsg;

namespace Content.Client.SS220.Administration.UI.Connections;

[UsedImplicitly]
public sealed class ConnectionLogsEui : BaseEui
{
    private readonly ConnectionLogsWindow _window;

    public ConnectionLogsEui()
    {
        _window = new ConnectionLogsWindow();
        _window.OnSearch += ckey => SendMessage(new Search(ckey));
        _window.OnNext += () => SendMessage(new NextPage());
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is ConnectionLogsEuiState connectionLogs)
            _window.SetState(connectionLogs);
    }
}
