using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration.Events;

[Serializable, NetSerializable]
public sealed class CustomObjectivesPlayersEvent : EntityEventArgs
{
    public List<CustomObjectivesPlayerInfo> Players = new();
}

[Serializable, NetSerializable]
public sealed record CustomObjectivesPlayerInfo(
    string Username,
    string CharacterName,
    string IdentityName,
    string StartingJob,
    NetEntity? NetEntity,
    NetUserId SessionId,
    bool Connected,
    bool ActiveThisRound,
    int ObjectiveCount);
