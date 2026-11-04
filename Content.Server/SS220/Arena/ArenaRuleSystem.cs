// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.SS220.AdmemeEvents.EventRole;
using Content.Shared.SS220.Arena.Lobby;
using Content.Shared.Station;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Arena;

public sealed partial class ArenaRuleSystem : GameRuleSystem<ArenaRuleComponent>
{
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStationSpawningSystem _stationSpawning = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private ISharedPlayerManager _players = default!;

    private static readonly SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg");
    private static readonly EntProtoId EffectSparks = "EffectSparks";

    private const int VictorySparkCount = 6;
    private const float VictorySparkOffsetRange = 0.6f;
    private const float VictoryLightRadius = 5f;
    private const float VictoryLightEnergy = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArenaParticipantComponent, MindAddedMessage>(OnParticipantMindAdded);
        SubscribeLocalEvent<ArenaParticipantComponent, MindRemovedMessage>(OnParticipantMindRemoved);
        SubscribeLocalEvent<ArenaParticipantComponent, MobStateChangedEvent>(OnParticipantStateChanged);
        SubscribeLocalEvent<ArenaParticipantComponent, ComponentRemove>(OnParticipantRemoved);
    }

    protected override void Started(EntityUid uid, ArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        TryCreateArena((uid, comp));
    }

    protected override void Ended(EntityUid uid, ArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        var ent = (uid, comp);
        if (comp.ArenaMapUid is { } mapUid && !TerminatingOrDeleted(mapUid))
            QueueDel(mapUid);

        ResetState(ent);
        comp.Phase = ArenaPhase.Disabled;
    }

    protected override void ActiveTick(EntityUid uid, ArenaRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        var ent = (uid, comp);
        switch (comp.Phase)
        {
            case ArenaPhase.WaitingForPlayers:
                UpdateWaitingTimeout(ent);
                break;
            case ArenaPhase.Countdown:
                UpdateCountdown(ent);
                break;
            case ArenaPhase.Fighting:
                UpdateFightTimeout(ent);
                break;
            case ArenaPhase.Resetting:
                UpdateResetting(ent);
                break;
        }
    }

    private bool TryCreateArena(Entity<ArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Phase != ArenaPhase.Disabled)
            return false;

        if (comp.Maps.Count == 0)
            return false;

        var entry = SelectNextMap(ent);
        comp.CurrentLoadout = entry.Loadout;
        comp.CurrentLoadouts = entry.Loadouts;
        comp.CurrentCountdown = entry.CountdownDuration;

        EntityUid mapUid;
        MapId mapId;
        try
        {
            mapUid = _maps.CreateMap(out mapId);
            _metaData.SetEntityName(mapUid, "ArenaMap");
        }
        catch (Exception e)
        {
            Log.Error($"Failed to create arena map: {e}");
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        if (!_loader.TryLoadGrid(mapId, new ResPath(entry.Path), out var gridRef))
        {
            Log.Error($"Failed to load arena grid from '{entry.Path}'.");
            QueueDel(mapUid);
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        comp.ArenaMapUid = mapUid;
        _metaData.SetEntityName(gridRef.Value.Owner, "ArenaGrid");

        CollectBarriers(ent);
        DiscoverTeams(ent);

        var needTeams = comp.Mode == ArenaGameMode.Creative ? 1 : 2;
        if (comp.Teams.Count < needTeams || comp.Teams.Values.Any(t => t.Capacity == 0))
        {
            Log.Error($"Arena grid '{entry.Path}' has insufficient teams (count={comp.Teams.Count}, mode={comp.Mode}).");
            QueueDel(mapUid);
            ResetState(ent);
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        comp.Phase = ArenaPhase.WaitingForPlayers;
        comp.WaitingEndAt = _timing.CurTime + comp.WaitingTimeout;

        var summary = string.Join("v", comp.Teams.OrderBy(kv => kv.Key).Select(kv => kv.Value.Capacity));
        Log.Info($"Arena ready. Map='{entry.Path}', Loadout={comp.CurrentLoadout}, Teams={summary}, Barriers={comp.Barriers.Count}.");
        return true;
    }

    private void DiscoverTeams(Entity<ArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        comp.Teams.Clear();

        var query = AllEntityQuery<ArenaParticipantComponent, TransformComponent>();
        while (query.MoveNext(out _, out var participant, out var xform))
        {
            if (xform.MapUid != comp.ArenaMapUid)
                continue;

            if (!comp.Teams.TryGetValue(participant.Team, out var team))
            {
                team = new ArenaTeam { Loadouts = ShuffleLoadouts(comp.CurrentLoadouts) };
                comp.Teams[participant.Team] = team;
            }
            team.Capacity++;
        }
    }

    private ArenaMapEntry SelectNextMap(Entity<ArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.SelectionMode == ArenaSelectionMode.Random)
            return _random.Pick(comp.Maps);

        var entry = comp.Maps[comp.CurrentMapIndex % comp.Maps.Count];
        comp.CurrentMapIndex = (comp.CurrentMapIndex + 1) % comp.Maps.Count;
        return entry;
    }

    private void CollectBarriers(Entity<ArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        comp.Barriers.Clear();
        var query = AllEntityQuery<ArenaFightBarrierComponent, TransformComponent>();
        while (query.MoveNext(out var bUid, out _, out var xform))
        {
            if (xform.MapUid == comp.ArenaMapUid)
                comp.Barriers.Add(bUid);
        }
    }

    private void OnParticipantMindAdded(Entity<ArenaParticipantComponent> ent, ref MindAddedMessage args)
    {
        var rule = FindRuleForMap(Transform(ent.Owner).MapUid);
        if (rule == null)
            return;

        var ruleEnt = rule.Value;
        if (!ruleEnt.Comp.Teams.TryGetValue(ent.Comp.Team, out var team))
            return;

        var isFirstTake = ruleEnt.Comp.Phase == ArenaPhase.WaitingForPlayers && !team.Members.Contains(ent.Owner);

        if (isFirstTake)
        {
            RegisterParticipant(ent.Owner, team, ent.Comp.Team);
            if (!ent.Comp.Equipped)
            {
                EquipLoadout(ruleEnt, ent.Owner, team, team.Members.Count - 1);
                ent.Comp.Equipped = true;
            }
            if (ruleEnt.Comp.Mode != ArenaGameMode.Creative)
                AssignTeamIcon(ent.Owner, ent.Comp);
        }

        if (ruleEnt.Comp.Mode != ArenaGameMode.Creative)
            ApplyPlayerName(ent.Owner, args.Mind.Comp.UserId);

        if (ruleEnt.Comp.Mode != ArenaGameMode.Creative
            && ruleEnt.Comp.Phase == ArenaPhase.WaitingForPlayers
            && ruleEnt.Comp.Teams.Values.All(t => t.Members.Count >= t.Capacity))
        {
            StartCountdown(ruleEnt);
        }
    }

    private Entity<ArenaRuleComponent>? FindRuleForMap(EntityUid? mapUid)
    {
        if (mapUid == null)
            return null;

        var ruleQuery = EntityQueryEnumerator<ArenaRuleComponent, GameRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleUid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleUid, gameRule))
                continue;
            if (rule.ArenaMapUid != mapUid)
                continue;
            return (ruleUid, rule);
        }
        return null;
    }

    private void RegisterParticipant(EntityUid uid, ArenaTeam team, string teamId)
    {
        if (team.Members.Contains(uid))
            return;
        if (team.Members.Count >= team.Capacity)
        {
            Log.Warning($"Team '{teamId}' is already full ({team.Members.Count}/{team.Capacity}), ignoring extra fighter {ToPrettyString(uid)}.");
            return;
        }

        team.Members.Add(uid);
        Log.Info($"Player registered: team={teamId}, entity={ToPrettyString(uid)} ({team.Members.Count}/{team.Capacity}).");
    }

    private void ApplyPlayerName(EntityUid fighter, NetUserId? userId)
    {
        if (userId is not { } id)
            return;
        if (!_players.TryGetSessionById(id, out var session))
            return;

        _metaData.SetEntityName(fighter, session.Name);
    }

    private void EquipLoadout(Entity<ArenaRuleComponent> ent, EntityUid fighter, ArenaTeam team, int memberIndex)
    {
        if (ResolveLoadout(ent, team, memberIndex) is not { } loadoutId)
            return;

        if (!_proto.TryIndex(loadoutId, out var gear))
        {
            Log.Warning($"Loadout '{loadoutId}' not found.");
            return;
        }

        _stationSpawning.EquipStartingGear(fighter, gear);
    }

    private static ProtoId<StartingGearPrototype>? ResolveLoadout(Entity<ArenaRuleComponent> ent, ArenaTeam team, int memberIndex)
    {
        if (team.Loadouts is { Count: > 0 } list)
            return list[memberIndex % list.Count];

        return ent.Comp.CurrentLoadout;
    }

    private List<ProtoId<StartingGearPrototype>>? ShuffleLoadouts(List<ProtoId<StartingGearPrototype>>? source)
    {
        if (source == null || source.Count == 0)
            return null;

        var copy = new List<ProtoId<StartingGearPrototype>>(source);
        _random.Shuffle(copy);
        return copy;
    }

    private void AssignTeamIcon(EntityUid fighter, ArenaParticipantComponent participant)
    {
        if (participant.Icon is not { } icon)
            return;

        var eventRole = EnsureComp<EventRoleComponent>(fighter);
        eventRole.StatusIcon = icon;
        eventRole.RoleGroupKey = $"ArenaTeam{participant.Team}";
        Dirty(fighter, eventRole);
    }

    private void OnParticipantStateChanged(Entity<ArenaParticipantComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead && args.NewMobState != MobState.Critical)
            return;

        var rule = FindRuleFor(ent.Owner);
        if (rule == null)
            return;

        if (rule.Value.Comp.Phase == ArenaPhase.Fighting)
            CheckWinCondition(rule.Value);
    }

    private void OnParticipantMindRemoved(Entity<ArenaParticipantComponent> ent, ref MindRemovedMessage args)
    {
        var rule = FindRuleFor(ent.Owner);
        if (rule == null)
            return;

        if (rule.Value.Comp.Phase != ArenaPhase.WaitingForPlayers)
            return;

        if (rule.Value.Comp.Teams.TryGetValue(ent.Comp.Team, out var team))
            team.Members.Remove(ent.Owner);
        Log.Info($"Player ghosted out of arena before start, freeing slot: entity={ToPrettyString(ent.Owner)}.");
    }

    private void OnParticipantRemoved(Entity<ArenaParticipantComponent> ent, ref ComponentRemove args)
    {
        var rule = FindRuleFor(ent.Owner);
        if (rule == null)
            return;

        if (rule.Value.Comp.Teams.TryGetValue(ent.Comp.Team, out var team))
            team.Members.Remove(ent.Owner);

        if (rule.Value.Comp.Phase == ArenaPhase.Fighting)
        {
            CheckWinCondition(rule.Value);
        }
        else if (rule.Value.Comp.Phase == ArenaPhase.Countdown)
        {
            var aliveTeams = CountAliveTeams(rule.Value, out var winningTeam);
            if (aliveTeams < 2)
                BeginReset(rule.Value, aliveTeams == 1 ? winningTeam : null);
        }
    }

    private Entity<ArenaRuleComponent>? FindRuleFor(EntityUid participant)
    {
        var query = EntityQueryEnumerator<ArenaRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;
            if (rule.Teams.Values.Any(t => t.Members.Contains(participant)))
                return (uid, rule);
        }
        return null;
    }

    private void StartCountdown(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        rule.Phase = ArenaPhase.Countdown;
        rule.WaitingEndAt = null;
        rule.CountdownEnd = _timing.CurTime + rule.CurrentCountdown;

        SendToParticipants(ent, Loc.GetString("arena-countdown-start", ("seconds", (int)rule.CurrentCountdown.TotalSeconds)));
        Log.Info($"Countdown started ({rule.CurrentCountdown.TotalSeconds}s).");
    }

    private void UpdateCountdown(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (_timing.CurTime < rule.CountdownEnd)
            return;

        var aliveTeams = CountAliveTeams(ent, out var winningTeam);
        if (aliveTeams < 2)
        {
            BeginReset(ent, aliveTeams == 1 ? winningTeam : null);
            return;
        }

        OpenFightBarriers(ent);
        rule.Phase = ArenaPhase.Fighting;
        rule.FightEndAt = _timing.CurTime + rule.MaxFightDuration;
        SendToParticipants(ent, Loc.GetString("arena-fight-start"));
        foreach (var fighter in EnumerateMembers(rule))
            PlayPingTo(fighter);
    }

    private void PlayPingTo(EntityUid fighter)
    {
        if (!TerminatingOrDeleted(fighter))
            _audio.PlayGlobal(PingSound, fighter);
    }

    private void ApplyVictoryEffects(EntityUid winner)
    {
        var light = _light.EnsureLight(winner);
        _light.SetColor(winner, Color.Gold, light);
        _light.SetRadius(winner, VictoryLightRadius, light);
        _light.SetEnergy(winner, VictoryLightEnergy, light);
        _light.SetEnabled(winner, true, light);

        var coords = Transform(winner).Coordinates;
        for (var i = 0; i < VictorySparkCount; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange),
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange));
            Spawn(EffectSparks, coords.Offset(offset));
        }
    }

    private void UpdateWaitingTimeout(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (rule.Mode == ArenaGameMode.Creative)
            return;
        if (rule.WaitingEndAt is not { } end || _timing.CurTime < end)
            return;

        SendToParticipants(ent, Loc.GetString("arena-waiting-timeout"));
        Log.Info($"Waiting timeout reached ({rule.WaitingTimeout.TotalSeconds}s), rerolling map.");
        BeginReset(ent, null);
    }

    private void UpdateFightTimeout(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (rule.FightEndAt is not { } end || _timing.CurTime < end)
            return;

        SendToParticipants(ent, Loc.GetString("arena-fight-timeout"));
        BeginReset(ent, null);
    }

    private void CheckWinCondition(Entity<ArenaRuleComponent> ent)
    {
        if (ent.Comp.Phase != ArenaPhase.Fighting)
            return;

        var alive = CountAliveTeams(ent, out var winningTeam);
        switch (alive)
        {
            case 0:
                BeginReset(ent, null);
                break;
            case 1:
                BeginReset(ent, winningTeam);
                break;
        }
    }

    private void OpenFightBarriers(Entity<ArenaRuleComponent> ent)
    {
        foreach (var b in ent.Comp.Barriers)
        {
            if (TerminatingOrDeleted(b))
                continue;

            QueueDel(b);
        }
    }

    private void BeginReset(Entity<ArenaRuleComponent> ent, ArenaTeam? winningTeam)
    {
        var rule = ent.Comp;
        if (rule.Phase == ArenaPhase.Resetting)
            return;

        rule.Phase = ArenaPhase.Resetting;
        rule.ResetReadyAt = _timing.CurTime + rule.ResetDelay;
        rule.PendingSpawn = false;

        if (winningTeam != null)
        {
            foreach (var winner in winningTeam.Members)
            {
                if (TerminatingOrDeleted(winner) || _mobState.IsDead(winner))
                    continue;
                SendToFighter(winner, Loc.GetString("arena-winner-popup"));
                PlayPingTo(winner);
                ApplyVictoryEffects(winner);
            }
        }

        Log.Info($"BeginReset: winnerTeam={(winningTeam != null ? "yes" : "none")}, delay={rule.ResetDelay.TotalSeconds}s.");
    }

    private void UpdateResetting(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (_timing.CurTime < rule.ResetReadyAt)
            return;

        if (!rule.PendingSpawn)
        {
            if (rule.ArenaMapUid is { } mapUid && !TerminatingOrDeleted(mapUid))
                QueueDel(mapUid);

            ResetState(ent);
            rule.PendingSpawn = true;
            rule.ResetReadyAt = _timing.CurTime + rule.RespawnDelay;
            return;
        }

        rule.PendingSpawn = false;
        rule.Phase = ArenaPhase.Disabled;

        if (rule.Lifecycle == ArenaLifecycle.DeleteOnKill)
        {
            GameTicker.EndGameRule(ent.Owner);
            QueueDel(ent.Owner);
            return;
        }

        TryCreateArena(ent);
    }

    private static void ResetState(Entity<ArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        rule.ArenaMapUid = null;
        rule.Teams.Clear();
        rule.CurrentLoadout = null;
        rule.CurrentLoadouts = null;
        rule.FightEndAt = null;
        rule.WaitingEndAt = null;
        rule.Barriers.Clear();
    }

    private int CountAliveTeams(Entity<ArenaRuleComponent> ent, out ArenaTeam? winningTeam)
    {
        winningTeam = null;
        var alive = 0;
        foreach (var team in ent.Comp.Teams.Values)
        {
            if (!HasAnyAlive(team.Members))
                continue;
            winningTeam = team;
            alive++;
        }
        return alive;
    }

    private bool HasAnyAlive(List<EntityUid> team)
    {
        return team.Any(uid => !TerminatingOrDeleted(uid) && _mobState.IsAlive(uid));
    }

    private static IEnumerable<EntityUid> EnumerateMembers(ArenaRuleComponent rule)
    {
        foreach (var team in rule.Teams.Values)
            foreach (var member in team.Members)
                yield return member;
    }

    private void SendToParticipants(Entity<ArenaRuleComponent> ent, string msg)
    {
        foreach (var fighter in EnumerateMembers(ent.Comp))
            SendToFighter(fighter, msg);
    }

    private void SendToFighter(EntityUid fighter, string msg)
    {
        if (TerminatingOrDeleted(fighter))
            return;

        _popup.PopupEntity(msg, fighter, fighter, PopupType.Large);

        if (!TryComp<ActorComponent>(fighter, out var actor))
            return;

        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chat.ChatMessageToOne(ChatChannel.Server, msg, wrapped, fighter, false, actor.PlayerSession.Channel);
    }
}
