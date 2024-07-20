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
using Content.Server.Bed.Cryostorage;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Robust.Server.Containers;
using Content.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.DoAfter;
using Content.Shared.SS220.TeleportAFKtoCryoSystem;
using Content.Shared.Administration.Logs;
using System.Globalization;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using Content.Shared.UserInterface;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Chat;
using Content.Shared.Climbing.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Mind.Components;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Forensics;
using Robust.Shared.Utility;

namespace Content.Server.SS220.TeleportAFKtoCryoSystem;

public sealed class TeleportAFKtoCryoSystem : EntitySystem
{
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _afkTeleportTocryo;

    private readonly Dictionary<(EntityUid, NetUserId), (TimeSpan, bool)> _entityEnteredSSDTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.AfkTeleportToCryo, SetAfkTeleportToCryo, true);
        _playerManager.PlayerStatusChanged += OnPlayerChange;
        SubscribeLocalEvent<CryostorageComponent, TeleportToCryoFinished>(OnTeleportFinished);
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
            if (pair.Value.Item2 && IsTeleportAfkToCryoTime(pair.Value.Item1)
                && TeleportEntityToCryoStorageWithDelay(pair.Key.Item1))
            {
                _entityEnteredSSDTimes.Remove(pair.Key);
            }
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
    public bool TeleportEntityToCryoStorageWithDelay(EntityUid target)
    {
        var station = _station.GetOwningStation(target);

        if (station is null)
            return false;

        foreach (var comp in EntityQuery<CryostorageComponent>())
        {
            if (comp.StoredPlayers.Contains(target))
                return true;
        }

        var cryopodSSDComponents = EntityQueryEnumerator<CryostorageComponent>();

        while (cryopodSSDComponents.MoveNext(out var cryopodSSDUid, out var cryopodSSDComp))
        {
            // if (cryopodSSDComp.BodyContainer.ContainedEntity is null
            if (!cryopodSSDComp.StoredPlayers.Contains(target)  // todo check
            && _station.GetOwningStation(cryopodSSDUid) == station)
            {
                var portal = Spawn("CryoStoragePortal", Transform(target).Coordinates);

                if (TryComp<AmbientSoundComponent>(portal, out var ambientSoundComponent))
                {
                    _audioSystem.PlayPvs(ambientSoundComponent.Sound, portal);
                }

                var doAfterArgs = new DoAfterArgs(EntityManager, target, TimeSpan.FromSeconds(4f), new TeleportToCryoFinished(GetNetEntity(portal)), cryopodSSDUid) // todo edit TimeSpan.FromSeconds(4)
                {
                    BreakOnDamage = false,
                    BreakOnMove = false,
                    NeedHand = false,
                };

                if (!_doAfterSystem.TryStartDoAfter(doAfterArgs))
                    QueueDel(portal);

                return true;
            }
        }

        return false;
    }

    private void OnTeleportFinished(Entity<CryostorageComponent> ent, ref TeleportToCryoFinished args)
    {
        if (_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            _adminLogger.Add(LogType.CryoStorage, LogImpact.High,
                $"{ToPrettyString(args.User):player} was teleported to cryostorage {ToPrettyString(ent)}");
            _container.Insert(args.User, container);
        }

        if (TryComp<CryostorageContainedComponent>(args.User, out var contained))
            contained.GracePeriodEndTime = _timing.CurTime + TimeSpan.Zero;

        var portalEntity = GetEntity(args.PortalId);

        if (TryComp<AmbientSoundComponent>(portalEntity, out var ambientSoundComponent))
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, portalEntity);

        EntityManager.DeleteEntity(portalEntity);
    }
}
