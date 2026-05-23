// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SS220.Arena.Lobby;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client.SS220.Arena.Lobby;

[UsedImplicitly]
public sealed class ArenaLobbyEui : BaseEui
{
    private readonly ArenaLobbyWindow _window;

    public ArenaLobbyEui()
    {
        _window = new ArenaLobbyWindow();
        _window.OnClose += () => SendMessage(new ArenaLobbyCloseMessage());
        _window.OnCreateRequested += protoId => SendMessage(new ArenaLobbyCreateMessage(protoId));
        _window.OnJoinRequested += id => SendMessage(new ArenaLobbyJoinMessage(id));
        _window.OnRefreshRequested += () => SendMessage(new ArenaLobbyRefreshMessage());
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not ArenaLobbyEuiState s)
            return;
        _window.Update(s);
    }
}
