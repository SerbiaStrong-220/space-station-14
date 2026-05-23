// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SS220.Arena.Lobby;

namespace Content.Server.SS220.Arena.Lobby;

public sealed class ArenaLobbyEui : BaseEui
{
    private readonly ArenaLobbySystem _system;

    public ArenaLobbyEui(ArenaLobbySystem system)
    {
        _system = system;
    }

    public override ArenaLobbyEuiState GetNewState()
    {
        return _system.BuildState(Player);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case ArenaLobbyCreateMessage create:
                _system.TryCreateArena(Player, create.ArenaProtoId);
                break;
            case ArenaLobbyJoinMessage join:
                _system.TryJoinArena(Player, join.ArenaId);
                break;
            case ArenaLobbyRefreshMessage:
                StateDirty();
                break;
        }
    }

    public override void Closed()
    {
        base.Closed();
        _system.CloseEui(Player);
    }
}
