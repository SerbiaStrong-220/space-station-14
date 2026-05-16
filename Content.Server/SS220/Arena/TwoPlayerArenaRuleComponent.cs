// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Robust.Shared.Map;
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
    public bool DeleteBarriers = true;

    public ArenaPhase Phase = ArenaPhase.Disabled;

    public EntityUid? ArenaMapUid;
    public MapId? ArenaMapId;
    public EntityUid? ArenaGridUid;

    [ViewVariables]
    public EntityUid? PlayerOne;

    [ViewVariables]
    public EntityUid? PlayerTwo;

    public TimeSpan? CountdownEnd;
    public TimeSpan? FightEndAt;
    public TimeSpan? ResetReadyAt;
    public bool PendingSpawn;
    public bool InReset;

    public int CurrentMapIndex;
    public ProtoId<StartingGearPrototype>? CurrentLoadout;
    public float CurrentCountdown;

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
    public float CountdownDuration = 10f;
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
