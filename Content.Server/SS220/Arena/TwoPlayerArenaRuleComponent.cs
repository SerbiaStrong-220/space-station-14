// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Arena;

[RegisterComponent, Access(typeof(TwoPlayerArenaRuleSystem))]
public sealed partial class TwoPlayerArenaRuleComponent : Component
{
    [DataField]
    public List<ArenaMapEntry> Maps = new();

    [DataField]
    public ArenaSelectionMode SelectionMode = ArenaSelectionMode.Rotation;

    [DataField]
    public TimeSpan ResetDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan MaxFightDuration = TimeSpan.FromSeconds(300);

    [DataField]
    public TimeSpan WaitingTimeout = TimeSpan.FromMinutes(2);

    [DataField]
    public TimeSpan RespawnDelay = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public int TeamASize;

    [ViewVariables]
    public int TeamBSize;

    [DataField]
    public ProtoId<FactionIconPrototype> TeamAIcon = "ArenaTeamAIcon";

    [DataField]
    public ProtoId<FactionIconPrototype> TeamBIcon = "ArenaTeamBIcon";

    public ArenaPhase Phase = ArenaPhase.Disabled;

    public EntityUid? ArenaMapUid;

    [ViewVariables]
    public List<EntityUid> TeamA = new();

    [ViewVariables]
    public List<EntityUid> TeamB = new();

    public TimeSpan? CountdownEnd;
    public TimeSpan? FightEndAt;
    public TimeSpan? WaitingEndAt;
    public TimeSpan? ResetReadyAt;
    public bool PendingSpawn;

    public int CurrentMapIndex;
    public ProtoId<StartingGearPrototype>? CurrentLoadout;
    public List<ProtoId<StartingGearPrototype>>? TeamALoadouts;
    public List<ProtoId<StartingGearPrototype>>? TeamBLoadouts;
    public TimeSpan CurrentCountdown;

    public readonly HashSet<EntityUid> Barriers = new();
}

[DataDefinition]
public sealed partial class ArenaMapEntry
{
    [DataField(required: true)]
    public string Path = string.Empty;

    [DataField]
    public ProtoId<StartingGearPrototype>? Loadout;

    [DataField]
    public List<ProtoId<StartingGearPrototype>>? Loadouts;

    [DataField]
    public TimeSpan CountdownDuration = TimeSpan.FromSeconds(10);
}

public enum ArenaPhase : byte
{
    Disabled = 0,
    WaitingForPlayers = 1,
    Countdown = 2,
    Fighting = 3,
    Resetting = 4,
}

public enum ArenaSelectionMode : byte
{
    Rotation = 0,
    Random = 1,
}
