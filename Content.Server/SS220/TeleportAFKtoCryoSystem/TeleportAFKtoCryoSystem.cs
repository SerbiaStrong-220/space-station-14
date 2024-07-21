// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared.Body.Components;
using Content.Shared.CCVar;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Bed.Cryostorage;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Robust.Server.Containers;
using Content.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.DoAfter;
using Content.Shared.SS220.TeleportAFKtoCryoSystem;
using Content.Shared.Administration.Logs;

namespace Content.Server.SS220.TeleportAFKtoCryoSystem;

public sealed class TeleportAFKtoCryoSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private float _afkTeleportTocryo;

    private readonly Dictionary<(EntityUid, NetUserId), (TimeSpan, bool)> _entityEnteredSSDTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.AfkTeleportToCryo, SetAfkTeleportToCryo, true);
        _playerManager.PlayerStatusChanged += OnPlayerChange;
        SubscribeLocalEvent<CryostorageComponent, TeleportToCryoFinished>(HandleTeleportFinished);
    }

    private void SetAfkTeleportToCryo(float value)
        => _afkTeleportTocryo = value;

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(CCVars.AfkTeleportToCryo, SetAfkTeleportToCryo);
        _playerManager.PlayerStatusChanged -= OnPlayerChange;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _entityEnteredSSDTimes.Where(uid => HasComp<MindContainerComponent>(uid.Key.Item1)))
        {
            if (pair.Value.Item2 && IsTeleportAfkToCryoTime(pair.Value.Item1) && TeleportEntityToCryoStorage(pair.Key.Item1))
                _entityEnteredSSDTimes.Remove(pair.Key);
        }
    }

    private bool IsTeleportAfkToCryoTime(TimeSpan time)
    {
        var timeOut = TimeSpan.FromSeconds(_afkTeleportTocryo);
        return _gameTiming.CurTime - time > timeOut;
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
                _entityEnteredSSDTimes[(e.Session.AttachedEntity.Value, e.Session.UserId)]
                    = (_gameTiming.CurTime, humanoidPreferences.TeleportAfkToCryoStorage);
                break;
            case SessionStatus.Connected:
                if (_entityEnteredSSDTimes
                    .TryFirstOrNull(item => item.Key.Item2 == e.Session.UserId, out var item))
                {
                    _entityEnteredSSDTimes.Remove(item.Value.Key);
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
        var station = _station.GetOwningStation(target);
        if (station is null)
            return false;

        if (TargetAlreadyInCryo(target))
            return true;

        var cryostorageComponents = EntityQueryEnumerator<CryostorageComponent>();
        while (cryostorageComponents.MoveNext(out var cryostorageUid, out var сryostorageComp))
        {
            if (TryTeleportToCryo(target, cryostorageUid, сryostorageComp.TeleportPortralID))
                return true;
        }

        return false;
    }

    private bool TargetAlreadyInCryo(EntityUid target)
    {
        return EntityQuery<CryostorageComponent>().Any(comp => comp.StoredPlayers.Contains(target));
    }

    private bool TryTeleportToCryo(EntityUid target, EntityUid cryopodUid, string teleportPortralID)
    {
        var portal = Spawn(teleportPortralID, Transform(target).Coordinates);

        if (TryComp<AmbientSoundComponent>(portal, out var ambientSoundComponent))
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, portal);

        var doAfterArgs = new DoAfterArgs(EntityManager, target, TimeSpan.FromSeconds(4f),
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
            contained.GracePeriodEndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(5f);

        var portalEntity = GetEntity(args.PortalId);

        if (TryComp<AmbientSoundComponent>(portalEntity, out var ambientSoundComponent))
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, portalEntity);

        EntityManager.DeleteEntity(portalEntity);
    }
}
