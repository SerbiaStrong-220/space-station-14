using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Administration.Events;

[Serializable, NetSerializable]
public sealed class CustomObjectivesPlayersEvent(List<CustomObjectivesPlayerInfo> players) : EntityEventArgs
{
    public readonly List<CustomObjectivesPlayerInfo> Players = players;
}

[Serializable, NetSerializable]
public readonly record struct CustomObjectivesPlayerInfo(
    string Username,
    string CharacterName,
    NetEntity? NetEntity,
    NetUserId SessionId,
    int ObjectiveCount);
