// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Arena.Lobby;

[RegisterComponent]
public sealed partial class ArenaLobbyEntryComponent : Component
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField(required: true)]
    public string MapPath = string.Empty;

    [DataField]
    public int MaxPlayers = 2;

    [DataField]
    public ProtoId<StartingGearPrototype>? Loadout;

    [DataField]
    public List<ProtoId<StartingGearPrototype>>? Loadouts;

    [DataField]
    public TimeSpan CountdownDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public string Category = "duel";

    [DataField]
    public ArenaGameMode Mode = ArenaGameMode.Duel;
}

public enum ArenaGameMode : byte
{
    Duel = 0,
    Creative = 1,
}
