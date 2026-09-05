// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Arena.Lobby;

[NetSerializable, Serializable]
public sealed class ArenaLobbyEuiState : EuiStateBase
{
    public List<ArenaLobbyEntry> Arenas { get; }
    public List<ArenaLobbyTemplate> Templates { get; }
    public int ActiveCount { get; }
    public int MaxArenas { get; }
    public bool HasOwnArena { get; }
    public int CreateCooldownRemaining { get; }

    public ArenaLobbyEuiState(List<ArenaLobbyEntry> arenas, List<ArenaLobbyTemplate> templates, int activeCount, int maxArenas, bool hasOwnArena, int createCooldownRemaining)
    {
        Arenas = arenas;
        Templates = templates;
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
    public ArenaPhase Phase;
    public string Category;
    public string Creator;
}

[NetSerializable, Serializable]
public struct ArenaLobbyTemplate
{
    public string Id;
    public string Name;
    public string Description;
    public string Category;
    public int MaxPlayers;
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
public sealed class ArenaLobbyObserveMessage : EuiMessageBase
{
    public uint ArenaId;

    public ArenaLobbyObserveMessage(uint arenaId)
    {
        ArenaId = arenaId;
    }
}
