// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Arena.Lobby;

[NetSerializable, Serializable]
public enum ArenaPhase : byte
{
    Disabled = 0,
    WaitingForPlayers = 1,
    Countdown = 2,
    Fighting = 3,
    Resetting = 4,
}

[NetSerializable, Serializable]
public enum ArenaGameMode : byte
{
    Duel = 0,
    Creative = 1,
}
