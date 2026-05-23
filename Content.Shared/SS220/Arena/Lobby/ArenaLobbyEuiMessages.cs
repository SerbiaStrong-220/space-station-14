// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Arena.Lobby;

[NetSerializable, Serializable]
public sealed class ArenaLobbyEuiState : EuiStateBase
{
    public List<ArenaLobbyEntry> Arenas { get; }
    public int ActiveCount { get; }
    public int MaxArenas { get; }
    public bool HasOwnArena { get; }
    public int CreateCooldownRemaining { get; }

    public ArenaLobbyEuiState(List<ArenaLobbyEntry> arenas, int activeCount, int maxArenas, bool hasOwnArena, int createCooldownRemaining)
    {
        Arenas = arenas;
        ActiveCount = activeCount;
        MaxArenas = maxArenas;
        HasOwnArena = hasOwnArena;
        CreateCooldownRemaining = createCooldownRemaining;
    }
}

[NetSerializable, Serializable]
public struct ArenaLobbyEntry
{
    public uint ArenaId;
    public string Name;
    public int Players;
    public int MaxPlayers;
    public ArenaLobbyStatus Status;
    public string Category;
}

[NetSerializable, Serializable]
public enum ArenaLobbyStatus : byte
{
    Waiting = 0,
    Countdown = 1,
    Fighting = 2,
    Finished = 3,
}

[NetSerializable, Serializable]
public sealed class ArenaLobbyCreateMessage : EuiMessageBase
{
    public string ArenaProtoId;

    public ArenaLobbyCreateMessage(string arenaProtoId)
    {
        ArenaProtoId = arenaProtoId;
    }
}

[NetSerializable, Serializable]
public sealed class ArenaLobbyJoinMessage : EuiMessageBase
{
    public uint ArenaId;

    public ArenaLobbyJoinMessage(uint arenaId)
    {
        ArenaId = arenaId;
    }
}

[NetSerializable, Serializable]
public sealed class ArenaLobbyRefreshMessage : EuiMessageBase
{
}

[NetSerializable, Serializable]
public sealed class ArenaLobbyCloseMessage : EuiMessageBase
{
}
