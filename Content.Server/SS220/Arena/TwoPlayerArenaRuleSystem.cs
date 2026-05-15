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
        SubscribeLocalEvent<ArenaParticipantComponent, MobStateChangedEvent>(OnParticipantStateChanged);
        SubscribeLocalEvent<ArenaParticipantComponent, ComponentRemove>(OnParticipantRemoved);
    }

    protected override void Started(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        TryCreateArena((uid, comp));
    }

    protected override void Ended(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        if (comp.ArenaMapUid is { } mapUid && !TerminatingOrDeleted(mapUid))
            QueueDel(mapUid);

        ResetState(comp);
        comp.Phase = ArenaPhase.Disabled;
    }

    protected override void ActiveTick(EntityUid uid, TwoPlayerArenaRuleComponent comp, GameRuleComponent gameRule, float frameTime)
    {
        var ent = (uid, comp);
        switch (comp.Phase)
        {
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

    private bool TryCreateArena(Entity<TwoPlayerArenaRuleComponent> rule)
    {
        var comp = rule.Comp;
        if (comp.Phase != ArenaPhase.Disabled)
            return false;

        if (comp.Maps.Count == 0)
        {
            _sawmill.Error("Arena rule has no maps configured.");
            return false;
        }

        var entry = SelectNextMap(comp);
        comp.CurrentLoadout = entry.Loadout;
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
        comp.ArenaMapId = mapId;
        comp.ArenaGridUid = gridRef.Value.Owner;
        _metaData.SetEntityName(comp.ArenaGridUid.Value, "TwoPlayerArenaGrid");

        CollectBarriers(comp);
        comp.Phase = ArenaPhase.WaitingForPlayers;

        _sawmill.Info($"Arena ready. Map='{entry.Path}', Loadout={comp.CurrentLoadout}, Barriers={comp.Barriers.Count}.");
        return true;
    }

    private ArenaMapEntry SelectNextMap(TwoPlayerArenaRuleComponent comp)
    {
        if (comp.SelectionMode == ArenaSelectionMode.Random)
            return _random.Pick(comp.Maps);

        var entry = comp.Maps[comp.CurrentMapIndex % comp.Maps.Count];
        comp.CurrentMapIndex = (comp.CurrentMapIndex + 1) % comp.Maps.Count;
        return entry;
    }

    private void CollectBarriers(TwoPlayerArenaRuleComponent comp)
    {
        comp.Barriers.Clear();
        var query = AllEntityQuery<ArenaFightBarrierComponent, TransformComponent>();
        while (query.MoveNext(out var bUid, out _, out var xform))
        {
            if (xform.GridUid == comp.ArenaGridUid)
                comp.Barriers.Add(bUid);
        }
    }

    private void OnParticipantMindAdded(Entity<ArenaParticipantComponent> ent, ref MindAddedMessage args)
    {
        var rule = FindRuleForGrid(Transform(ent.Owner).GridUid);
        if (rule == null)
            return;

        var isFirstTake = rule.Phase == ArenaPhase.WaitingForPlayers
            && (ent.Comp.Slot == ArenaSlot.PlayerOne ? rule.PlayerOne != ent.Owner : rule.PlayerTwo != ent.Owner);

        if (isFirstTake)
        {
            RegisterParticipant(rule, ent.Owner, ent.Comp.Slot);
            EquipLoadout(rule, ent.Owner);
        }

        ApplyPlayerName(ent.Owner, args.Mind.Comp.UserId);

        if (rule.Phase == ArenaPhase.WaitingForPlayers && rule.PlayerOne.HasValue && rule.PlayerTwo.HasValue)
            StartCountdown(rule);
    }

    private TwoPlayerArenaRuleComponent? FindRuleForGrid(EntityUid? gridUid)
    {
        if (gridUid == null)
            return null;

        var ruleQuery = EntityQueryEnumerator<TwoPlayerArenaRuleComponent, GameRuleComponent>();
        while (ruleQuery.MoveNext(out var ruleUid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(ruleUid, gameRule))
                continue;
            if (rule.ArenaGridUid != gridUid)
                continue;
            return rule;
        }
        return null;
    }

    private void RegisterParticipant(TwoPlayerArenaRuleComponent rule, EntityUid uid, ArenaSlot slot)
    {
        if (slot == ArenaSlot.PlayerOne)
        {
            if (rule.PlayerOne.HasValue && rule.PlayerOne != uid)
                return;
            rule.PlayerOne = uid;
        }
        else
        {
            if (rule.PlayerTwo.HasValue && rule.PlayerTwo != uid)
                return;
            rule.PlayerTwo = uid;
        }

        _sawmill.Info($"Player registered: slot={slot}, entity={uid}.");
    }

    private void ApplyPlayerName(EntityUid fighter, NetUserId? userId)
    {
        if (userId is not { } id)
            return;
        if (!_players.TryGetSessionById(id, out var session))
            return;

        _metaData.SetEntityName(fighter, session.Name);
    }

    private void EquipLoadout(TwoPlayerArenaRuleComponent rule, EntityUid fighter)
    {
        if (rule.CurrentLoadout is not { } loadoutId)
            return;

        if (!_proto.TryIndex(loadoutId, out var gear))
        {
            _sawmill.Warning($"Loadout '{loadoutId}' not found.");
            return;
        }

        _stationSpawning.EquipStartingGear(fighter, gear);
    }

    private void OnParticipantStateChanged(Entity<ArenaParticipantComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var rule = FindRuleFor(ent.Owner);
        if (rule == null)
            return;

        switch (rule.Phase)
        {
            case ArenaPhase.Countdown:
                break;
            case ArenaPhase.Fighting:
                CheckWinCondition(rule);
                break;
        }
    }

    private void OnParticipantRemoved(Entity<ArenaParticipantComponent> ent, ref ComponentRemove args)
    {
        var rule = FindRuleFor(ent.Owner);
        if (rule == null || rule.InReset)
            return;

        switch (rule.Phase)
        {
            case ArenaPhase.WaitingForPlayers:
                if (rule.PlayerOne == ent.Owner)
                    rule.PlayerOne = null;
                if (rule.PlayerTwo == ent.Owner)
                    rule.PlayerTwo = null;
                break;
            case ArenaPhase.Countdown:
                break;
            case ArenaPhase.Fighting:
                CheckWinCondition(rule);
                break;
        }
    }

    private TwoPlayerArenaRuleComponent? FindRuleFor(EntityUid participant)
    {
        var query = EntityQueryEnumerator<TwoPlayerArenaRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;
            if (rule.PlayerOne == participant || rule.PlayerTwo == participant)
                return rule;
        }
        return null;
    }

    private void StartCountdown(TwoPlayerArenaRuleComponent rule)
    {
        rule.Phase = ArenaPhase.Countdown;
        rule.CountdownEnd = _timing.CurTime + TimeSpan.FromSeconds(rule.CurrentCountdown);

        SendToParticipants(rule, Loc.GetString("arena-countdown-start", ("seconds", (int)rule.CurrentCountdown)));
        _sawmill.Info($"Countdown started ({rule.CurrentCountdown}s).");
    }

    private void UpdateCountdown(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (_timing.CurTime < rule.CountdownEnd)
            return;

        var aliveCount = CountAlive(rule, requireActor: false, out var lastAlive);
        if (aliveCount < 2)
        {
            BeginReset(rule, aliveCount == 1 ? lastAlive : null);
            return;
        }

        OpenFightBarriers(rule);
        rule.Phase = ArenaPhase.Fighting;
        rule.FightEndAt = _timing.CurTime + rule.MaxFightDuration;
        SendToParticipants(rule, Loc.GetString("arena-fight-start"));
        PlayPingTo(rule.PlayerOne);
        PlayPingTo(rule.PlayerTwo);
    }

    private void PlayPingTo(EntityUid? uid)
    {
        if (uid is { } fighter && !TerminatingOrDeleted(fighter))
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
            var offset = new System.Numerics.Vector2(
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange),
                _random.NextFloat(-VictorySparkOffsetRange, VictorySparkOffsetRange));
            Spawn(EffectSparks, coords.Offset(offset));
        }
    }

    private void UpdateFightTimeout(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (rule.FightEndAt is not { } end || _timing.CurTime < end)
            return;

        SendToParticipants(rule, Loc.GetString("arena-fight-timeout"));
        BeginReset(rule, null);
    }

    private void CheckWinCondition(TwoPlayerArenaRuleComponent rule)
    {
        if (rule.Phase != ArenaPhase.Fighting)
            return;

        var alive = CountAlive(rule, requireActor: false, out var lastAlive);
        switch (alive)
        {
            case 0:
                BeginReset(rule, null);
                break;
            case 1:
                BeginReset(rule, lastAlive);
                break;
        }
    }

    private void OpenFightBarriers(TwoPlayerArenaRuleComponent rule)
    {
        foreach (var b in rule.Barriers)
        {
            if (TerminatingOrDeleted(b))
                continue;

            QueueDel(b);
        }
    }

    private void BeginReset(TwoPlayerArenaRuleComponent rule, EntityUid? winner)
    {
        if (rule.Phase == ArenaPhase.Resetting || rule.InReset)
            return;

        rule.Phase = ArenaPhase.Resetting;
        rule.ResetReadyAt = _timing.CurTime + rule.ResetDelay;
        rule.PendingSpawn = false;

        if (winner.HasValue && !TerminatingOrDeleted(winner.Value))
        {
            SendToFighter(winner, Loc.GetString("arena-winner-popup"));
            PlayPingTo(winner);
            ApplyVictoryEffects(winner.Value);
        }

        _sawmill.Info($"BeginReset: winner={winner}, delay={rule.ResetDelay.TotalSeconds}s.");
    }

    private void UpdateResetting(Entity<TwoPlayerArenaRuleComponent> ent)
    {
        var rule = ent.Comp;
        if (_timing.CurTime < rule.ResetReadyAt)
            return;

        if (!rule.PendingSpawn)
        {
            rule.InReset = true;
            if (rule.ArenaMapUid is { } mapUid && !TerminatingOrDeleted(mapUid))
                QueueDel(mapUid);

            ResetState(rule);
            rule.InReset = false;
            rule.PendingSpawn = true;
            rule.ResetReadyAt = _timing.CurTime + TimeSpan.FromSeconds(2);
            return;
        }

        rule.PendingSpawn = false;
        rule.Phase = ArenaPhase.Disabled;
        TryCreateArena(ent);
    }

    private static void ResetState(TwoPlayerArenaRuleComponent rule)
    {
        rule.ArenaMapUid = null;
        rule.ArenaMapId = null;
        rule.ArenaGridUid = null;
        rule.PlayerOne = null;
        rule.PlayerTwo = null;
        rule.CurrentLoadout = null;
        rule.FightEndAt = null;
        rule.Barriers.Clear();
    }

    private int CountAlive(TwoPlayerArenaRuleComponent rule, bool requireActor, out EntityUid lastAlive)
    {
        lastAlive = default;
        var count = 0;
        if (IsAlive(rule.PlayerOne, requireActor))
        {
            lastAlive = rule.PlayerOne!.Value;
            count++;
        }
        if (IsAlive(rule.PlayerTwo, requireActor))
        {
            lastAlive = rule.PlayerTwo!.Value;
            count++;
        }
        return count;
    }

    private bool IsAlive(EntityUid? uid, bool requireActor)
    {
        if (!uid.HasValue)
            return false;
        if (TerminatingOrDeleted(uid.Value))
            return false;
        if (_mobState.IsDead(uid.Value))
            return false;
        if (requireActor && !HasComp<ActorComponent>(uid.Value))
            return false;
        return true;
    }

    private void SendToParticipants(TwoPlayerArenaRuleComponent rule, string msg)
    {
        SendToFighter(rule.PlayerOne, msg);
        SendToFighter(rule.PlayerTwo, msg);
    }

    private void SendToFighter(EntityUid? uid, string msg)
    {
        if (uid is not { } fighter || TerminatingOrDeleted(fighter))
            return;

        _popup.PopupEntity(msg, fighter, fighter, PopupType.Large);

        if (!TryComp<ActorComponent>(fighter, out var actor))
            return;

        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
        _chat.ChatMessageToOne(ChatChannel.Server, msg, wrapped, fighter, false, actor.PlayerSession.Channel);
    }
}
