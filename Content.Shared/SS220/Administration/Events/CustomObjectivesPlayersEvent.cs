using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration.Events;

[Serializable, NetSerializable]
public sealed class CustomObjectivesPlayersEvent : EntityEventArgs
{
    public List<CustomObjectivesPlayerInfo> Players;

    public CustomObjectivesPlayersEvent(List<CustomObjectivesPlayerInfo> players)
    {
        Players = players;
    }
}

[Serializable, NetSerializable]
public readonly record struct CustomObjectivesPlayerInfo(
    string Username,
    string CharacterName,
    string IdentityName,
    string StartingJob,
    NetEntity? NetEntity,
    NetUserId SessionId,
    bool Connected,
    bool ActiveThisRound,
    int ObjectiveCount);
