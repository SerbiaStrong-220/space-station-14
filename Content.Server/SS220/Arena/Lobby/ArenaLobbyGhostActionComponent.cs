// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Arena.Lobby;

[RegisterComponent, Access(typeof(ArenaLobbyGhostActionSystem))]
public sealed partial class ArenaLobbyGhostActionComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionGhostOpenArenaLobby";

    [DataField]
    public EntityUid? ActionEntity;
}
