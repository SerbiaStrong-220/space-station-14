using Content.Shared.Roles;
using Content.Shared.SS220.Arena;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.SS220.Arena;

[RegisterComponent, Access(typeof(TwoPlayerArenaRuleSystem))]
public sealed partial class TwoPlayerArenaRuleComponent : Component
{
    [DataField]
    public List<ArenaMapEntry> Maps = new();

    [DataField]
    public ArenaSelectionMode SelectionMode = ArenaSelectionMode.Rotation;

    [DataField]
    public float CountdownDuration = 60f;

    [DataField]
    public float ResetDelay = 5f;

    [DataField]
    public float MaxFightDuration = 300f;

    [DataField]
    public bool DeleteBarriers = true;

    public ArenaPhase Phase = ArenaPhase.Disabled;

    public EntityUid? ArenaMapUid;
    public MapId? ArenaMapId;
    public EntityUid? ArenaGridUid;

    public EntityUid? PlayerOne;
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
    public float? CountdownDuration;
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
