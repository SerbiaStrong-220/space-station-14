// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.Arena.Lobby;
using Content.Shared.SS220.CCVars;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Arena.Lobby;

public sealed class ArenaLobbySystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const float RefreshIntervalSeconds = 3f;

    private uint _nextArenaId = 1;
    private float _refreshAccumulator;

    private readonly Dictionary<ICommonSession, ArenaLobbyEui> _openUis = new();
    private readonly Dictionary<uint, EntityUid> _arenas = new();
    private readonly Dictionary<uint, NetUserId> _arenaCreators = new();
    private readonly HashSet<uint> _arenaWasJoined = new();
    private readonly Dictionary<NetUserId, TimeSpan> _createCooldownUntil = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<ArenaRuleComponent, EntityTerminatingEvent>(OnArenaTerminating);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _refreshAccumulator += frameTime;
        if (_refreshAccumulator < RefreshIntervalSeconds)
            return;

        _refreshAccumulator = 0f;
        EndEmptyCreativeArenas();
        _refreshAccumulator = 0f;
        EndEmptyCreativeArenas();

        if (_openUis.Count == 0)
            return;

        RefreshAll();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _arenas.Clear();
        _arenaCreators.Clear();
        _arenaWasJoined.Clear();
        _createCooldownUntil.Clear();
        foreach (var eui in _openUis.Values.ToArray())
            eui.Close();
        _openUis.Clear();
        _nextArenaId = 1;
    }

    private void OnArenaTerminating(Entity<ArenaRuleComponent> ent, ref EntityTerminatingEvent args)
    {
        uint? toRemove = null;
        foreach (var (id, uid) in _arenas)
        {
            if (uid != ent.Owner)
                continue;
            toRemove = id;
            break;
        }
        if (toRemove is not { } id2)
            return;

        _arenas.Remove(id2);
        _arenaCreators.Remove(id2);
        _arenaWasJoined.Remove(id2);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _openUis.Remove(args.Session);
    }

    public void OpenEuiFor(ICommonSession session)
    {
        if (session.AttachedEntity is not { } attached || !HasComp<GhostComponent>(attached))
            return;

        if (_openUis.TryGetValue(session, out var existing))
        {
            existing.StateDirty();
            return;
        }

        var eui = new ArenaLobbyEui(this);
        _openUis[session] = eui;
        _euiManager.OpenEui(eui, session);
        eui.StateDirty();
    }

    public void CloseEui(ICommonSession session)
    {
        _openUis.Remove(session);
    }

    public ArenaLobbyEuiState BuildState(ICommonSession viewer)
    {
        var hasOwnArena = _arenaCreators.ContainsValue(viewer.UserId);
        var arenas = new List<ArenaLobbyEntry>(_arenas.Count);
        foreach (var (id, ruleUid) in _arenas)
        {
            if (!TryComp<ArenaRuleComponent>(ruleUid, out var rule))
            {
                Log.Error($"Arena id={id} references {ToPrettyString(ruleUid)} without {nameof(ArenaRuleComponent)}; cleanup missed.");
                continue;
            }

            var creatorName = string.Empty;
            if (_arenaCreators.TryGetValue(id, out var creatorId)
                && _playerManager.TryGetPlayerData(creatorId, out var creatorData))
            {
                creatorName = creatorData.UserName;
            }

            arenas.Add(new ArenaLobbyEntry
            {
                ArenaId = id,
                Name = rule.DisplayName,
                Players = CountOccupied(rule),
                MaxPlayers = rule.MaxPlayers,
                Phase = rule.Phase,
                Category = rule.DisplayCategory,
                Creator = creatorName,
            });
        }

        var templates = new List<ArenaLobbyTemplate>();
        foreach (var proto in _proto.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;
            if (!proto.TryGetComponent<ArenaRuleComponent>(out var rule, _factory) || !rule.ShowInLobby)
                continue;

            templates.Add(new ArenaLobbyTemplate
            {
                Id = proto.ID,
                Name = rule.DisplayName,
                Description = rule.Description,
                Category = rule.DisplayCategory,
                MaxPlayers = rule.MaxPlayers,
            });
        }

        return new ArenaLobbyEuiState(arenas, templates, _arenas.Count, _cfg.GetCVar(CCVars220.ArenaActiveLimit), hasOwnArena, GetCooldownRemaining(viewer.UserId));
    }

    private int GetCooldownRemaining(NetUserId userId)
    {
        if (!_createCooldownUntil.TryGetValue(userId, out var until))
            return 0;

        var remaining = (until - _gameTiming.CurTime).TotalSeconds;
        return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
    }

    public void TryCreateArena(ICommonSession session, string arenaProtoId)
    {
        var max = _cfg.GetCVar(CCVars220.ArenaActiveLimit);
        if (_arenas.Count >= max)
            return;

        if (_arenaCreators.ContainsValue(session.UserId))
            return;

        if (GetCooldownRemaining(session.UserId) > 0)
            return;

        if (!_proto.TryIndex<EntityPrototype>(arenaProtoId, out var entryProto)
            || !entryProto.TryGetComponent<ArenaRuleComponent>(out var protoRule, _factory)
            || !protoRule.ShowInLobby)
        {
            Log.Warning($"Unknown arena lobby entry '{arenaProtoId}'.");
            return;
        }

        var ruleUid = _gameTicker.AddGameRule(arenaProtoId);
        if (!TryComp<ArenaRuleComponent>(ruleUid, out var rule))
        {
            Log.Error($"'{arenaProtoId}' prototype is missing {nameof(ArenaRuleComponent)}.");
            QueueDel(ruleUid);
            return;
        }

        if (!_gameTicker.StartGameRule(ruleUid) || rule.Phase == ArenaPhase.Disabled)
        {
            Log.Error($"Arena start failed: proto={arenaProtoId}.");
            _gameTicker.EndGameRule(ruleUid);
            QueueDel(ruleUid);
            return;
        }

        if (!JoinFreeSlot(session, ruleUid, rule))
        {
            Log.Warning($"Arena create aborted: host {session.Name} could not join.");
            _gameTicker.EndGameRule(ruleUid);
            QueueDel(ruleUid);
            return;
        }

        var id = _nextArenaId++;
        _arenas[id] = ruleUid;
        _arenaCreators[id] = session.UserId;
        _arenaWasJoined.Add(id);
        _createCooldownUntil[session.UserId] = _gameTiming.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars220.ArenaCreateCooldown));
        Log.Info($"Arena created: id={id}, proto={arenaProtoId}, host={session.Name}.");
        CloseEuiFor(session);
        RefreshAll();
    }

    public bool TryObserveArena(ICommonSession session, uint arenaId)
    {
        if (session.AttachedEntity is not { } ghost || !HasComp<GhostComponent>(ghost))
            return false;

        if (!_arenas.TryGetValue(arenaId, out var ruleUid))
            return false;

        if (!TryComp<ArenaRuleComponent>(ruleUid, out var rule))
        {
            Log.Error($"Arena id={arenaId} references {ToPrettyString(ruleUid)} without {nameof(ArenaRuleComponent)}; cleanup missed.");
            return false;
        }

        if (rule.ArenaMapUid is not { } mapUid || TerminatingOrDeleted(mapUid))
        {
            Log.Error($"Arena id={arenaId} has no usable map (uid={rule.ArenaMapUid}).");
            return false;
        }

        _transform.SetCoordinates(ghost, new EntityCoordinates(mapUid, Vector2.Zero));
        return true;
    }

    public void TryJoinArena(ICommonSession session, uint arenaId)
    {
        if (!_arenas.TryGetValue(arenaId, out var ruleUid)
            || !TryComp<ArenaRuleComponent>(ruleUid, out var rule))
        {
            return;
        }

        if (!IsJoinablePhase(rule.Phase))
            return;

        if (!JoinFreeSlot(session, ruleUid, rule))
            return;

        _arenaWasJoined.Add(arenaId);
        CloseEuiFor(session);
        RefreshAll();
    }

    private void CloseEuiFor(ICommonSession session)
    {
        if (_openUis.TryGetValue(session, out var eui))
            eui.Close();
    }

    public void RefreshAll()
    {
        foreach (var eui in _openUis.Values)
            eui.StateDirty();
    }

    private static bool IsJoinablePhase(ArenaPhase phase)
    {
        return phase is ArenaPhase.WaitingForPlayers;
    }

    private bool JoinFreeSlot(ICommonSession session, EntityUid ruleUid, ArenaRuleComponent rule)
    {
        var body = FindFreeBody(rule);
        if (body == null)
        {
            Log.Info($"Arena join denied (no free body): rule={ToPrettyString(ruleUid)}, player={session.Name}.");
            return false;
        }

        WipePlayerMind(session);
        WipeStaleMindOn(body.Value);

        if (rule.Mode == ArenaGameMode.Creative)
            body = ReplaceWithPlayerCharacter(session, body.Value);

        var mind = _mindSystem.CreateMind(session.UserId, Name(body.Value));
        _mindSystem.MakeSentient(body.Value);
        _mindSystem.TransferTo(mind, body.Value);

        Log.Info($"Arena join: rule={ToPrettyString(ruleUid)}, body={ToPrettyString(body.Value)}, player={session.Name}.");
        return true;
    }

    private EntityUid ReplaceWithPlayerCharacter(ICommonSession session, EntityUid oldBody)
    {
        var profile = _gameTicker.GetPlayerProfile(session);
        var oldPart = CompOrNull<ArenaParticipantComponent>(oldBody);
        var coords = Transform(oldBody).Coordinates;
        QueueDel(oldBody);

        var newBody = _stationSpawning.SpawnPlayerMob(coords, null, profile, null);
        var part = EnsureComp<ArenaParticipantComponent>(newBody);
        part.Team = oldPart?.Team ?? "a";
        part.Icon = oldPart?.Icon;
        return newBody;
    }

    private void WipePlayerMind(ICommonSession session)
    {
        if (_mindSystem.TryGetMind(session.UserId, out _, out var mind) && !mind.IsVisitingEntity)
            _mindSystem.WipeMind(session);
    }

    private void WipeStaleMindOn(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var container) || !container.HasMind)
            return;

        if (!TryComp<MindComponent>(container.Mind, out var mind))
            return;

        if (mind.VisitingEntity != null)
            _mindSystem.UnVisit(container.Mind.Value, mind);

        _mindSystem.WipeMind(container.Mind.Value, mind);
    }

    private int CountOccupied(ArenaRuleComponent rule)
    {
        var count = 0;
        foreach (var team in rule.Teams.Values)
        {
            foreach (var member in team.Members)
            {
                if (!TerminatingOrDeleted(member) && IsBodyOccupied(member))
                    count++;
            }
        }
        return count;
    }

    private EntityUid? FindFreeBody(ArenaRuleComponent rule)
    {
        if (rule.ArenaMapUid is not { } mapUid)
            return null;

        var query = AllEntityQuery<ArenaParticipantComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == mapUid && !TerminatingOrDeleted(uid) && !IsBodyOccupied(uid))
                return uid;
        }
        return null;
    }

    private bool IsBodyOccupied(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var container) || !container.HasMind)
            return false;

        if (!TryComp<MindComponent>(container.Mind, out var mind) || mind.IsVisitingEntity)
            return false;

        return mind.UserId.HasValue && _playerManager.TryGetSessionById(mind.UserId.Value, out _);
    }

    private void EndEmptyCreativeArenas()
    {
        List<EntityUid>? toEnd = null;
        foreach (var (id, ruleUid) in _arenas)
        {
            if (!_arenaWasJoined.Contains(id))
                continue;

            if (!TryComp<ArenaRuleComponent>(ruleUid, out var rule) || rule.Mode != ArenaGameMode.Creative)
                continue;

            if (CountOccupied(rule) > 0)
                continue;

            toEnd ??= new List<EntityUid>();
            toEnd.Add(ruleUid);
        }

        if (toEnd == null)
            return;

        foreach (var uid in toEnd)
        {
            Log.Info($"Creative arena empty, ending: {ToPrettyString(uid)}.");
            _gameTicker.EndGameRule(uid);
            QueueDel(uid);
        }
    }
}
