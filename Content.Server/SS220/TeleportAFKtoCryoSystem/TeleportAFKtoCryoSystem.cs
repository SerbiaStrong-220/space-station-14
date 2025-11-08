// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TeleportAFKtoCryoSystem;
using Robust.Server.Containers;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.SS220.TeleportAFKtoCryoSystem;

public sealed class TeleportAFKtoCryoSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly GhostSystem _ghostSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private TimeSpan _afkTeleportToCryo;
    private TimeSpan _ssdTimeout;

    private readonly Dictionary<(EntityUid, NetUserId), TimeSpan> _entityEnteredSSDTimes = new();
    private readonly List<(EntityUid, NetUserId)> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars220.AfkTeleportToCryo, SetAfkTeleportToCryo, true);
        _cfg.OnValueChanged(CCVars220.SDDTimeOut, SetSSDTimeout, true);

        _playerManager.PlayerStatusChanged += OnPlayerChange;

        SubscribeLocalEvent<CryostorageComponent, TeleportToCryoFinished>(HandleTeleportFinished);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);
    }

    private void SetAfkTeleportToCryo(float value)
        => _afkTeleportToCryo = TimeSpan.FromSeconds(value);

    private void SetSSDTimeout(float value)
        => _ssdTimeout = TimeSpan.FromSeconds(value);

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        _entityEnteredSSDTimes.Clear();
        _toRemove.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars220.AfkTeleportToCryo, SetAfkTeleportToCryo);
        _cfg.UnsubValueChanged(CCVars220.SDDTimeOut, SetSSDTimeout);

        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_entityEnteredSSDTimes.Count == 0)
            return;

        foreach (var key in _entityEnteredSSDTimes.Keys.AsEnumerable())
        {
            if (Deleted(key.Item1))
                _entityEnteredSSDTimes.Remove(key);
        }

        _toRemove.Clear();

        foreach (var pair in _entityEnteredSSDTimes)
        {
            if (pair.Value + _ssdTimeout < _gameTiming.CurTime)
            {
                Log.Error($"Got entry in ssd dictionary for too long! Entity is {ToPrettyString(pair.Key.Item1)} and user is {pair.Key.Item2}");
                _toRemove.Add(pair.Key);
                continue;
            }

            if (!IsTeleportAfkToCryoTime(pair.Value))
                continue;

            if (!TeleportEntityToCryoStorage(pair.Key.Item1))
                Log.Warning($"Cant find any cryo for {ToPrettyString(pair.Key.Item1)}! Removing from ssd teleport queue...");

            _toRemove.Add(pair.Key);
        }

        foreach (var key in _toRemove)
        {
            _entityEnteredSSDTimes.Remove(key);
        }
    }

    private bool IsTeleportAfkToCryoTime(TimeSpan time)
    {
        return _gameTiming.CurTime - time > _afkTeleportToCryo;
    }

    private void OnPlayerChange(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                if (e.Session.AttachedEntity is null
                    || !HasComp<MindContainerComponent>(e.Session.AttachedEntity)
                    || !HasComp<BodyComponent>(e.Session.AttachedEntity))
                {
                    break;
                }

                if (!_preferencesManager.TryGetCachedPreferences(e.Session.UserId, out var preferences)
                    || preferences.SelectedCharacter is not HumanoidCharacterProfile humanoidPreferences)
                {
                    break;
                }

                if (!humanoidPreferences.TeleportAfkToCryoStorage)
                    break;

                _entityEnteredSSDTimes[(e.Session.AttachedEntity.Value, e.Session.UserId)] = _gameTiming.CurTime;
                break;
            case SessionStatus.Connected:
                foreach (var keys in _entityEnteredSSDTimes.Keys.ToList())
                {
                    if (keys.Item2 == e.Session.UserId)
                        _entityEnteredSSDTimes.Remove(keys);
                }
                break;
        }
    }

    /// <summary>
    /// Tries to teleport target inside cryopod, if any available
    /// </summary>
    /// <param name="target"> Target to teleport in first matching cryopod</param>
    /// <returns> true if player successfully transferred to cryo storage, otherwise returns false</returns>
    public bool TeleportEntityToCryoStorage(EntityUid target)
    {
        if (!TryComp(target, out TransformComponent? xform))
            return false;

        var station = _station.GetOwningStation(target, xform);
        if (station is null)
            return false;

        if (TargetAlreadyInCryo(target))
            return true;

        HashSet<Entity<CryostorageComponent>> cryoStorageOnGrid = new();
        _entityLookup.GetGridEntities(station.Value, cryoStorageOnGrid);

        foreach (var cryo in cryoStorageOnGrid)
        {
            if (TryTeleportToCryo(target, cryo, station.Value, cryo.Comp.TeleportPortralID))
                return true;
        }

        return false;
    }

    private bool TargetAlreadyInCryo(EntityUid target)
    {
        return EntityQuery<CryostorageComponent>().Any(comp => comp.StoredPlayers.Contains(target));
    }

    private bool TryTeleportToCryo(EntityUid target, EntityUid cryopodUid, EntityUid station, string teleportPortralID)
    {
        if (station != _station.GetOwningStation(cryopodUid))
            return false;

        // Kicks the mind out of the entity if it cannot enter the cryostorage
        if (!HasComp<CanEnterCryostorageComponent>(target))
        {
            if (_mindSystem.GetMind(target) is { } mind)
            {
                _ghostSystem.OnGhostAttempt(mind, false);
            }
            return true;
        }

        var portal = Spawn(teleportPortralID, Transform(target).Coordinates);

        if (TryComp<AmbientSoundComponent>(portal, out var ambientSoundComponent))
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, portal);

        var doAfterArgs = new DoAfterArgs(EntityManager, target, TimeSpan.FromSeconds(4),
            new TeleportToCryoFinished(GetNetEntity(portal)), cryopodUid)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            NeedHand = false
        };

        if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
        {
            QueueDel(portal);
            return false;
        }

        return true;
    }

    private void HandleTeleportFinished(Entity<CryostorageComponent> ent, ref TeleportToCryoFinished args)
    {
        if (_containerSystem.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            _adminLogger.Add(LogType.CryoStorage, LogImpact.High,
                $"{ToPrettyString(args.User):player} was teleported to cryostorage {ent}");
            _containerSystem.Insert(args.User, container);
        }

        if (TryComp<CryostorageContainedComponent>(args.User, out var contained))
            contained.GracePeriodEndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(1);

        var portalEntity = GetEntity(args.PortalId);

        if (TryComp<AmbientSoundComponent>(portalEntity, out var ambientSoundComponent))
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, portalEntity);

        EntityManager.QueueDeleteEntity(portalEntity);
    }
}
