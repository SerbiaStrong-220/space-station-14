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
using System.Numerics;

namespace Content.Server.SS220.Arena;

public sealed class TwoPlayerArenaRuleSystem : GameRuleSystem<TwoPlayerArenaRuleComponent>
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;

    private static readonly SoundSpecifier PingSound = new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg");
    private static readonly EntProtoId EffectSparks = "EffectSparks";

    private const string TeamARoleGroupKey = "ArenaTeamA";
    private const string TeamBRoleGroupKey = "ArenaTeamB";

    private const int VictorySparkCount = 6;
    private const float VictorySparkOffsetRange = 0.6f;
    private const float VictoryLightRadius = 5f;
    private const float VictoryLightEnergy = 4f;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("arena");

        SubscribeLocalEvent<ArenaParticipantComponent, MindAddedMessage>(OnParticipantMindAdded);
        SubscribeLocalEvent<ArenaParticipantComponent, MindRemovedMessage>(OnParticipantMindRemoved);
        SubscribeLocalEvent<ArenaParticipantComponent, MobStateChangedEvent>(OnParticipantStateChanged);
        SubscribeLocalEvent<ArenaParticipantComponent, ComponentRemove>(OnParticipantRemoved);
    }

    protected override void Started(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        TryCreateArena((uid, comp));
    }

    protected override void Ended(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        var ent = (uid, comp);
        if (comp.ArenaMapUid is { } mapUid && !TerminatingOrDeleted(mapUid))
            QueueDel(mapUid);

        ResetState(ent);
        comp.Phase = ArenaPhase.Disabled;
    }

    protected override void ActiveTick(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, float frameTime)
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

    private bool TryCreateArena(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Phase != ArenaPhase.Disabled)
            return false;

        if (comp.Maps.Count == 0)
        {
            _sawmill.Error("Arena rule has no maps configured.");
            return false;
        }

        var entry = SelectNextMap(ent);
        comp.CurrentLoadout = entry.Loadout;
        comp.TeamALoadouts = ShuffleLoadouts(entry.Loadouts);
        comp.TeamBLoadouts = ShuffleLoadouts(entry.Loadouts);
        comp.CurrentCountdown = entry.CountdownDuration;

        EntityUid mapUid;
        MapId mapId;
        try
        {
            mapUid = _maps.CreateMap(out mapId);
            _metaData.SetEntityName(mapUid, "TwoPlayerArena");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to create arena map: {e}");
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        if (!_loader.TryLoadGrid(mapId, new ResPath(entry.Path), out var gridRef))
        {
            _sawmill.Error($"Failed to load arena grid from '{entry.Path}'.");
            QueueDel(mapUid);
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        comp.ArenaMapUid = mapUid;
        _metaData.SetEntityName(gridRef.Value.Owner, "TwoPlayerArenaGrid");

        CollectBarriers(ent);
        CountTeamFighters(ent);

        if (comp.TeamASize == 0 || comp.TeamBSize == 0)
        {
            _sawmill.Error($"Arena grid '{entry.Path}' has no fighters for one of the teams (A={comp.TeamASize}, B={comp.TeamBSize}).");
            QueueDel(mapUid);
            ResetState(ent);
            comp.Phase = ArenaPhase.Disabled;
            return false;
        }

        comp.Phase = ArenaPhase.WaitingForPlayers;
        comp.WaitingEndAt = _timing.CurTime + comp.WaitingTimeout;

        _sawmill.Info($"Arena ready. Map='{entry.Path}', Loadout={comp.CurrentLoadout}, Teams={comp.TeamASize}v{comp.TeamBSize}, Barriers={comp.Barriers.Count}.");
        return true;
    }

    private void CountTeamFighters(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        comp.TeamASize = 0;
        comp.TeamBSize = 0;

        var query = AllEntityQuery<ArenaParticipantComponent, TransformComponent>();
        while (query.MoveNext(out _, out var participant, out var xform))
        {
            if (xform.MapUid != comp.ArenaMapUid)
                continue;

            if (participant.Slot == ArenaSlot.PlayerOne)
                comp.TeamASize++;
            else
                comp.TeamBSize++;
        }
    }

    private ArenaMapEntry SelectNextMap(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.SelectionMode == ArenaSelectionMode.Random)
            return _random.Pick(comp.Maps);

        var entry = comp.Maps[comp.CurrentMapIndex % comp.Maps.Count];
        comp.CurrentMapIndex = (comp.CurrentMapIndex + 1) % comp.Maps.Count;
        return entry;
    }

    private void CollectBarriers(Entity<TwoPlayerArenaRuleComponent> ent)
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
        var team = GetTeam(ruleEnt, ent.Comp.Slot);
        var isFirstTake = ruleEnt.Comp.Phase == ArenaPhase.WaitingForPlayers && !team.Contains(ent.Owner);

        if (isFirstTake)
        {
            RegisterParticipant(ruleEnt, ent.Owner, ent.Comp.Slot);
            if (!ent.Comp.Equipped)
            {
                EquipLoadout(ruleEnt, ent.Owner, ent.Comp.Slot, team.Count - 1);
                ent.Comp.Equipped = true;
            }
            AssignTeamIcon(ruleEnt, ent.Owner, ent.Comp.Slot);
        }

        ApplyPlayerName(ent.Owner, args.Mind.Comp.UserId);

        if (ruleEnt.Comp.Phase == ArenaPhase.WaitingForPlayers
            && ruleEnt.Comp.TeamA.Count >= ruleEnt.Comp.TeamASize
            && ruleEnt.Comp.TeamB.Count >= ruleEnt.Comp.TeamBSize)
        {
            StartCountdown(ruleEnt);
        }
    }

    private Entity<TwoPlayerArenaRuleComponent>? FindRuleForMap(EntityUid? mapUid)
    {
        if (mapUid == null)
            return null;

        var ruleQuery = EntityQueryEnumerator<TwoPlayerArenaRuleComponent, GameRuleComponent>();
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

    private static List<EntityUid> GetTeam(Entity<TwoPlayerArenaRuleComponent> ent, ArenaSlot slot)
    {
        return slot == ArenaSlot.PlayerOne ? ent.Comp.TeamA : ent.Comp.TeamB;
    }

    private void RegisterParticipant(Entity<TwoPlayerArenaRuleComponent> ent, EntityUid uid, ArenaSlot slot)
    {
        var team = GetTeam(ent, slot);
        var capacity = slot == ArenaSlot.PlayerOne ? ent.Comp.TeamASize : ent.Comp.TeamBSize;

        if (team.Contains(uid))
            return;
        if (team.Count >= capacity)
        {
            _sawmill.Warning($"Team {slot} is already full ({team.Count}/{capacity}), ignoring extra fighter {uid}.");
            return;
        }

        team.Add(uid);
        _sawmill.Info($"Player registered: team={slot}, entity={uid} ({team.Count}/{capacity}).");
    }

    private void ApplyPlayerName(EntityUid fighter, NetUserId? userId)
    {
        if (userId is not { } id)
            return;
        if (!_players.TryGetSessionById(id, out var session))
            return;

        _metaData.SetEntityName(fighter, session.Name);
    }

    private void EquipLoadout(Entity<TwoPlayerArenaRuleComponent> ent, EntityUid fighter, ArenaSlot slot, int teamIndex)
    {
        if (ResolveLoadout(ent, slot, teamIndex) is not { } loadoutId)
            return;

        if (!_proto.TryIndex(loadoutId, out var gear))
        {
            _sawmill.Warning($"Loadout '{loadoutId}' not found.");
            return;
        }

        _stationSpawning.EquipStartingGear(fighter, gear);
    }

    private static ProtoId<StartingGearPrototype>? ResolveLoadout(Entity<TwoPlayerArenaRuleComponent> ent, ArenaSlot slot, int teamIndex)
    {
        var list = slot == ArenaSlot.PlayerOne ? ent.Comp.TeamALoadouts : ent.Comp.TeamBLoadouts;
        if (list is { Count: > 0 })
            return list[teamIndex % list.Count];

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

    private void AssignTeamIcon(Entity<TwoPlayerArenaRuleComponent> ent, EntityUid fighter, ArenaSlot slot)
    {
        var eventRole = EnsureComp<EventRoleComponent>(fighter);
        eventRole.StatusIcon = slot == ArenaSlot.PlayerOne ? ent.Comp.TeamAIcon : ent.Comp.TeamBIcon;
        eventRole.RoleGroupKey = slot == ArenaSlot.PlayerOne ? TeamARoleGroupKey : TeamBRoleGroupKey;
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

        GetTeam(rule.Value, ent.Comp.Slot).Remove(ent.Owner);
        _sawmill.Info($"Player ghosted out of arena before start, freeing slot: entity={ent.Owner}.");
    }

    private void OnParticipantRemoved(Entity<ArenaParticipantComponent> ent, ref ComponentRemove args)
    {
        var rule = FindRuleFor(ent.Owner);
        if (rule == null)
            return;

        if (rule.Value.Comp.Phase == ArenaPhase.WaitingForPlayers)
        {
            GetTeam(rule.Value, ent.Comp.Slot).Remove(ent.Owner);
        }
        else if (rule.Value.Comp.Phase == ArenaPhase.Fighting)
        {
            CheckWinCondition(rule.Value);
        }
    }

    private Entity<TwoPlayerArenaRuleComponent>? FindRuleFor(EntityUid participant)
    {
        var query = EntityQueryEnumerator<TwoPlayerArenaRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;
            if (rule.TeamA.Contains(participant) || rule.TeamB.Contains(participant))
                return (uid, rule);
        }
        return null;
    }

    private void StartCountdown(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        rule.Phase = ArenaPhase.Countdown;
        rule.WaitingEndAt = null;
        rule.CountdownEnd = _timing.CurTime + rule.CurrentCountdown;

        SendToParticipants(ent, Loc.GetString("arena-countdown-start", ("seconds", (int)rule.CurrentCountdown.TotalSeconds)));
        _sawmill.Info($"Countdown started ({rule.CurrentCountdown.TotalSeconds}s).");
    }

    private void UpdateCountdown(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (_timing.CurTime < rule.CountdownEnd)
            return;

        var aliveTeams = CountAliveTeams(ent, out var winningSlot);
        if (aliveTeams < 2)
        {
            BeginReset(ent, aliveTeams == 1 ? winningSlot : null);
            return;
        }

        OpenFightBarriers(ent);
        rule.Phase = ArenaPhase.Fighting;
        rule.FightEndAt = _timing.CurTime + rule.MaxFightDuration;
        SendToParticipants(ent, Loc.GetString("arena-fight-start"));
        foreach (var fighter in rule.TeamA)
            PlayPingTo(fighter);
        foreach (var fighter in rule.TeamB)
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

    private void UpdateWaitingTimeout(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (rule.WaitingEndAt is not { } end || _timing.CurTime < end)
            return;

        SendToParticipants(ent, Loc.GetString("arena-waiting-timeout"));
        _sawmill.Info($"Waiting timeout reached ({rule.WaitingTimeout.TotalSeconds}s), rerolling map.");
        BeginReset(ent, null);
    }

    private void UpdateFightTimeout(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (rule.FightEndAt is not { } end || _timing.CurTime < end)
            return;

        SendToParticipants(ent, Loc.GetString("arena-fight-timeout"));
        BeginReset(ent, null);
    }

    private void CheckWinCondition(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        if (ent.Comp.Phase != ArenaPhase.Fighting)
            return;

        var alive = CountAliveTeams(ent, out var winningSlot);
        switch (alive)
        {
            case 0:
                BeginReset(ent, null);
                break;
            case 1:
                BeginReset(ent, winningSlot);
                break;
        }
    }

    private void OpenFightBarriers(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        foreach (var b in ent.Comp.Barriers)
        {
            if (TerminatingOrDeleted(b))
                continue;

            QueueDel(b);
        }
    }

    private void BeginReset(Entity<TwoPlayerArenaRuleComponent> ent, ArenaSlot? winningSlot)
    {
        var rule = ent.Comp;
        if (rule.Phase == ArenaPhase.Resetting)
            return;

        rule.Phase = ArenaPhase.Resetting;
        rule.ResetReadyAt = _timing.CurTime + rule.ResetDelay;
        rule.PendingSpawn = false;

        if (winningSlot.HasValue)
        {
            var winners = GetTeam(ent, winningSlot.Value);
            foreach (var winner in winners)
            {
                if (TerminatingOrDeleted(winner) || _mobState.IsDead(winner))
                    continue;
                SendToFighter(winner, Loc.GetString("arena-winner-popup"));
                PlayPingTo(winner);
                ApplyVictoryEffects(winner);
            }
        }

        _sawmill.Info($"BeginReset: winningTeam={winningSlot}, delay={rule.ResetDelay.TotalSeconds}s.");
    }

    private void UpdateResetting(Entity<TwoPlayerArenaRuleComponent> ent)
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
        TryCreateArena(ent);
    }

    private static void ResetState(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        rule.ArenaMapUid = null;
        rule.TeamA.Clear();
        rule.TeamB.Clear();
        rule.TeamASize = 0;
        rule.TeamBSize = 0;
        rule.CurrentLoadout = null;
        rule.TeamALoadouts = null;
        rule.TeamBLoadouts = null;
        rule.FightEndAt = null;
        rule.WaitingEndAt = null;
        rule.Barriers.Clear();
    }

    private int CountAliveTeams(Entity<TwoPlayerArenaRuleComponent> ent, out ArenaSlot winningSlot)
    {
        winningSlot = default;
        var alive = 0;
        if (HasAnyAlive(ent.Comp.TeamA))
        {
            winningSlot = ArenaSlot.PlayerOne;
            alive++;
        }
        if (HasAnyAlive(ent.Comp.TeamB))
        {
            winningSlot = ArenaSlot.PlayerTwo;
            alive++;
        }
        return alive;
    }

    private bool HasAnyAlive(List<EntityUid> team)
    {
        foreach (var uid in team)
        {
            if (IsAlive(uid))
                return true;
        }
        return false;
    }

    private bool IsAlive(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return false;
        if (_mobState.IsDead(uid) || _mobState.IsCritical(uid))
            return false;
        return true;
    }

    private void SendToParticipants(Entity<TwoPlayerArenaRuleComponent> ent, string msg)
    {
        foreach (var fighter in ent.Comp.TeamA)
            SendToFighter(fighter, msg);
        foreach (var fighter in ent.Comp.TeamB)
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
