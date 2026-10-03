// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Mind;
using Content.Server.Power.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Administration;
using Content.Shared.APC;
using Content.Shared.GameTicking;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Power.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.SS220.MalfAI;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;
    [Dependency] private SharedStoreSystem _store = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SiliconLawSystem _laws = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private StationAiVisionSystem _vision = default!;

    private static readonly EntProtoId StoreProto = "MalfAiStore";

    private static readonly EntProtoId[] InnateActionPrototypes =
    {
        "ActionMalfAiHackApc",
        "ActionMalfAiOpenStore",
    };

    private readonly HashSet<EntityUid> _deadStores = new();
    private readonly Dictionary<EntityUid, (TimeSpan At, float Size)> _visionExpansionCache = new();
    private TimeSpan _nextApcPayout;
    private readonly Dictionary<EntityUid, int> _apcPayoutCounts = new();
    private readonly Dictionary<string, FixedPoint2> _apcPayoutCurrency = new();

    private static readonly TimeSpan VisionExpansionTtl = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan VisionExpansionMaxAge = TimeSpan.FromSeconds(5);

    private static readonly EntProtoId HijackObjectiveProto = "MalfAiHijackShuttleObjective";

    private static readonly LocId DenyNoRole = "malfai-module-no-role";
    private static readonly LocId DenyNotOperational = "malfai-module-not-operational";
    private static readonly LocId DenyDisabled = "malfai-module-disabled";
    private static readonly LocId DenyNoStation = "malfai-module-no-station";

    private static readonly LocId AbilityProtected = "malfai-ability-protected";
    private static readonly LocId HackNoPower = "malfai-hack-no-power";
    private static readonly LocId AbilityNoVision = "malfai-ability-no-vision";
    private static readonly LocId HackAlready = "malfai-hack-already";
    private static readonly LocId HackNoStore = "malfai-hack-no-store";
    private static readonly LocId HackSuccess = "malfai-hack-success";
    private static readonly LocId ModuleRefunded = "malfai-module-refunded";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemoved);
        SubscribeLocalEvent<MindComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<MalfAiRoleComponent, ComponentShutdown>(OnMalfRoleShutdown);
        SubscribeLocalEvent<MalfAiActorComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiHackApcEvent>(OnHackApc);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiOpenStoreEvent>(OnOpenStore);
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnStoreBought);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _deadStores.Clear();
        _visionExpansionCache.Clear();
        _nextApcPayout = TimeSpan.Zero;
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (!_roles.MindHasRole<MalfAiRoleComponent>(args.MindId, out var role))
            return;

        role.Value.Comp2.OwnerMind = args.MindId;

        if (args.Mind.OwnedEntity is not { } body || HasComp<GhostComponent>(body))
            return;

        SetupMalfBody(args.MindId, body);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        var body = args.Entity;
        if (HasComp<GhostComponent>(body))
            return;

        if (!_mind.TryGetMind(body, out var mindId, out _)
            || !_roles.MindHasRole<MalfAiRoleComponent>(mindId))
            return;

        if (!TryComp<MalfAiActorComponent>(body, out var actor) || actor.Mind != mindId)
            SetupMalfBody(mindId, body);
    }

    private void OnMindGotAdded(Entity<MindComponent> ent, ref MindGotAddedEvent args)
    {
        var body = args.Container.Owner;
        if (!HasComp<GhostComponent>(body) && _roles.MindHasRole<MalfAiRoleComponent>(ent.Owner))
            SetupMalfBody(ent.Owner, body);
    }

    private void OnMindRemoved(Entity<MalfAiActorComponent> ent, ref MindRemovedMessage args)
    {
        if (ent.Comp.Mind == args.Mind.Owner)
            CleanupBody(ent, args.Mind.Owner);
    }

    private void OnMalfRoleShutdown(Entity<MalfAiRoleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.OwnerMind is not { } mindId)
            return;

        if (!TryComp<MindComponent>(mindId, out var mind))
            return;

        if (HasLiveMalfRole(mindId))
            return;

        if (mind.OwnedEntity is { } body)
            CleanupBody(body, mindId);

        RemoveMalfObjectives(mindId, mind);
        ClearHackedApcs(mindId);
        DeleteOwnedStore(mindId);
    }

    private void RemoveMalfObjectives(EntityUid mindId, MindComponent mind)
    {
        for (var i = mind.Objectives.Count - 1; i >= 0; i--)
        {
            var objective = mind.Objectives[i];
            if (!TryComp(objective, out MetaDataComponent? meta)
                || meta.EntityPrototype?.ID != HijackObjectiveProto.Id)
                continue;

            _mind.TryRemoveObjective(mindId, mind, objective);
        }
    }

    private void CleanupBody(EntityUid body, EntityUid mindId)
    {
        RemoveProtectedLaw(body);
        TeardownRemoteStore(body, mindId);
        RemoveActor(body, mindId);
    }

    private void SetupMalfBody(EntityUid mindId, EntityUid body)
    {
        InstallProtectedLaw(body);

        var store = EnsureStore(mindId);
        SetupRemoteStore(body, mindId, store.Owner);
        EnsureComp<ActionsContainerComponent>(body);
        GrantActions(body, mindId);
        GrantMindActions(mindId, body);

        if (ResolvePerformerStation(body) is { } station
            && TryComp<MalfAiCameraUpgradeComponent>(station, out var upgrade))
            GrantThermalVision(body, upgrade);
    }

    private void OnRoleRemoved(RoleRemovedEvent args)
    {
        if (HasLiveMalfRole(args.MindId))
            return;

        var currentBody = args.Mind.OwnedEntity;
        if (currentBody is { } body)
            CleanupBody(body, args.MindId);

        var stale = new List<EntityUid>();
        var query = EntityQueryEnumerator<MalfAiActorComponent>();
        while (query.MoveNext(out var uid, out var actor))
        {
            if (uid != currentBody && actor.Mind == args.MindId)
                stale.Add(uid);
        }

        foreach (var uid in stale)
            CleanupBody(uid, args.MindId);

        ClearHackedApcs(args.MindId);
        RemoveMalfObjectives(args.MindId, args.Mind);
        DeleteOwnedStore(args.MindId);
    }

    internal bool HasLiveMalfRole(EntityUid mindId)
    {
        if (!TryComp<MindComponent>(mindId, out var mind))
            return false;

        foreach (var roleEnt in mind.MindRoleContainer.ContainedEntities)
        {
            if (TerminatingOrDeleted(roleEnt))
                continue;

            if (HasComp<MalfAiRoleComponent>(roleEnt))
                return true;
        }

        return false;
    }

    public void InstallProtectedLaw(EntityUid body)
    {
        _laws.InstallMalfLaw(body);
        _admin.Add(LogType.Action, LogImpact.High,
            $"Malf AI zero law installed on {ToPrettyString(body):target}.");
    }

    public void RemoveProtectedLaw(EntityUid body)
    {
        _laws.RemoveMalfLaw(body);
    }

    private void OnHackApc(Entity<MalfAiActorComponent> ent, ref MalfAiHackApcEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        if (!TryGetMalfContext(performer, out var ctx, out var denyReason))
        {
            if (denyReason != DenyNoRole)
                _popup.PopupEntity(Loc.GetString(denyReason), performer, performer);
            return;
        }

        var mindId = ctx.Value.MindId;
        var role = ctx.Value.Role;

        if (TerminatingOrDeleted(target) || !TryComp<ApcComponent>(target, out var apc))
            return;

        if (HasComp<HighPriorityAPCComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityProtected), performer, performer);
            return;
        }

        if (!apc.MainBreakerEnabled || _battery.GetCharge(target) <= 0f)
        {
            _popup.PopupEntity(Loc.GetString(HackNoPower), performer, performer);
            return;
        }

        if (!IsVisibleToAi(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityNoVision), performer, performer);
            return;
        }

        if (HasComp<MalfAiHackedApcComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString(HackAlready), performer, performer);
            return;
        }

        if (!TryGetOwnedStore(mindId, out _))
        {
            _popup.PopupEntity(Loc.GetString(HackNoStore), performer, performer);
            return;
        }

        var reward = FixedPoint2.New(_cfg.GetCVar(CCVars220.MalfAiApcReward));
        var rewardInterval = GetApcRewardInterval();
        var hackedApc = EnsureComp<MalfAiHackedApcComponent>(target);
        hackedApc.OwnerMind = mindId;
        var hackedAt = _timing.CurTime;
        hackedApc.NextRewardAt = _nextApcPayout > hackedAt ? _nextApcPayout : hackedAt + rewardInterval;
        _appearance.SetData(target, ApcVisuals.Hacked, true);

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString(HackSuccess,
            ("reward", reward),
            ("interval", rewardInterval.TotalSeconds)), performer, performer);
        _admin.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(performer):player} (Malf AI) hacked {ToPrettyString(target):target}; " +
            $"it generates {reward} {MalfAiConstants.CpuCurrency} " +
            $"every {rewardInterval.TotalSeconds} seconds.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Count<MalfAiHackedApcComponent>() == 0)
            return;

        var now = _timing.CurTime;
        var interval = GetApcRewardInterval();
        if (now < _nextApcPayout)
            return;

        _nextApcPayout = now + interval;

        var rewardPerApc = FixedPoint2.New(_cfg.GetCVar(CCVars220.MalfAiApcReward));
        _apcPayoutCounts.Clear();
        var payoutQuery = EntityQueryEnumerator<MalfAiHackedApcComponent>();
        while (payoutQuery.MoveNext(out _, out var hacked))
        {
            if (hacked.NextRewardAt > now)
                continue;

            hacked.NextRewardAt = now + interval;

            if (TerminatingOrDeleted(hacked.OwnerMind)
                || !HasLiveMalfRole(hacked.OwnerMind)
                || !TryGetOwnedStore(hacked.OwnerMind, out _))
                continue;

            _apcPayoutCounts.TryGetValue(hacked.OwnerMind, out var count);
            _apcPayoutCounts[hacked.OwnerMind] = count + 1;
        }

        _apcPayoutCurrency.Clear();
        foreach (var (mindId, count) in _apcPayoutCounts)
        {
            if (!TryGetOwnedStore(mindId, out var store))
                continue;

            _apcPayoutCurrency[MalfAiConstants.CpuCurrency] = rewardPerApc * count;
            _store.TryAddCurrency(_apcPayoutCurrency, store.Value.Owner, store.Value.Comp);
        }
    }

    private void ClearHackedApcs(EntityUid mindId)
    {
        var hacked = new List<EntityUid>();
        var query = EntityQueryEnumerator<MalfAiHackedApcComponent>();
        while (query.MoveNext(out var apc, out var component))
        {
            if (component.OwnerMind == mindId)
                hacked.Add(apc);
        }

        foreach (var apc in hacked)
            RemoveHackedApc(apc);
    }

    private void RemoveHackedApc(EntityUid apc)
    {
        if (TerminatingOrDeleted(apc))
            return;

        RemComp<MalfAiHackedApcComponent>(apc);
        _appearance.SetData(apc, ApcVisuals.Hacked, false);
    }

    private TimeSpan GetApcRewardInterval()
    {
        return TimeSpan.FromSeconds(Math.Max(1, _cfg.GetCVar(CCVars220.MalfAiApcRewardInterval)));
    }

    public EntityUid? ResolvePerformerStation(EntityUid performer)
    {
        if (_stationAi.TryGetCore(performer, out var core))
            return _station.GetOwningStation(core.Owner);
        return _station.GetOwningStation(performer);
    }

    public bool TryGetMalfContext(EntityUid performer,
        [NotNullWhen(true)] out MalfExecutionContext? context,
        [NotNullWhen(false)] out string? denyReason)
    {
        context = null;
        denyReason = null;

        if (!_mind.TryGetMind(performer, out var mindId, out _)
            || !_roles.MindHasRole<MalfAiRoleComponent>(mindId, out var role))
        {
            denyReason = DenyNoRole;
            return false;
        }

        if (!_cfg.GetCVar(CCVars220.MalfAiEnabled))
        {
            denyReason = DenyDisabled;
            return false;
        }

        if (!IsMalfOperational(performer))
        {
            denyReason = DenyNotOperational;
            return false;
        }

        var station = ResolvePerformerStation(performer);
        if (station == null)
        {
            denyReason = DenyNoStation;
            return false;
        }

        context = new MalfExecutionContext(performer, mindId, role.Value, station.Value);
        return true;
    }

    public bool TryGetMalfContext(EntityUid performer,
        [NotNullWhen(true)] out MalfExecutionContext? context)
    {
        if (TryGetMalfContext(performer, out context, out var denyReason))
            return true;

        _popup.PopupEntity(Loc.GetString(denyReason), performer, performer);
        return false;
    }

    public bool IsMalfOperational(EntityUid performer)
    {
        if (!_stationAi.TryGetCore(performer, out _))
            return false;

        if (!TryComp<MalfAiActorComponent>(performer, out var actor))
            return false;

        if (!_mind.TryGetMind(performer, out var mindId, out _)
            || actor.Mind != mindId)
            return false;

        if (TryComp<MobStateComponent>(performer, out var mob)
            && (_mobState.IsDead(performer, mob) || _mobState.IsCritical(performer, mob)))
            return false;

        return true;
    }

    public bool IsVisibleToAi(EntityUid target, TransformComponent? targetXform = null)
    {
        targetXform ??= Transform(target);
        return IsVisibleToAi(targetXform.Coordinates, targetXform);
    }

    public bool IsVisibleToAi(EntityCoordinates coords, TransformComponent? coordsXform = null)
    {
        if (!coords.IsValid(EntityManager))
            return false;

        coordsXform ??= Transform(coords.EntityId);
        if (coordsXform.GridUid is not { } gridUid)
            return false;

        if (!TryComp<BroadphaseComponent>(gridUid, out var broadphase)
            || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.LocalToTile(gridUid, grid, coords);
        return _vision.IsAccessible((gridUid, broadphase, grid), tile,
            expansionSize: GetGridVisionExpansion(gridUid), fastPath: false);
    }

    private float GetGridVisionExpansion(EntityUid gridUid)
    {
        var now = _timing.CurTime;
        if (_visionExpansionCache.TryGetValue(gridUid, out var cached) && now - cached.At < VisionExpansionTtl)
            return cached.Size;

        var size = _vision.GetExpansionSize(gridUid);
        _visionExpansionCache[gridUid] = (now, size);

        List<EntityUid>? stale = null;
        foreach (var (grid, entry) in _visionExpansionCache)
        {
            if (now - entry.At >= VisionExpansionMaxAge)
                (stale ??= new List<EntityUid>()).Add(grid);
        }

        if (stale != null)
        {
            foreach (var grid in stale)
            {
                _visionExpansionCache.Remove(grid);
            }
        }

        return size;
    }

    public Entity<StoreComponent> EnsureStore(EntityUid mindId)
    {
        if (TryGetOwnedStore(mindId, out var cached))
            return cached.Value;

        var store = Spawn(StoreProto, MapCoordinates.Nullspace);
        var comp = Comp<StoreComponent>(store);
        comp.AccountOwner = mindId;
        var starting = _cfg.GetCVar(CCVars220.MalfAiStartingCpu);
        if (starting > 0)
        {
            _store.TryAddCurrency(
                new Dictionary<string, FixedPoint2> { { MalfAiConstants.CpuCurrency, FixedPoint2.New(starting) } },
                store, comp);
        }
        _store.RefreshAllListings(comp);
        ApplyFeatureToggles(comp);
        PinOwnedStore(mindId, store);
        return (store, comp);
    }

    private void ApplyFeatureToggles(StoreComponent store)
    {
        if (!_cfg.GetCVar(CCVars220.MalfAiLockdownEnabled))
            RemoveListing(store, MalfAiConstants.LockdownListing);

        if (!_cfg.GetCVar(CCVars220.MalfAiRobotFactoryEnabled))
            RemoveListing(store, MalfAiConstants.RobotFactoryListing);

        if (!_cfg.GetCVar(CCVars220.MalfAiDoomsdayEnabled))
            RemoveListing(store, MalfAiConstants.DoomsdayListing);
    }

    private static void RemoveListing(StoreComponent store, ProtoId<ListingPrototype> listingId)
    {
        store.FullListingsCatalog.RemoveWhere(data => data.ID == listingId.Id);
    }

    public bool TryGetOwnedStore(EntityUid mindId, [NotNullWhen(true)] out Entity<StoreComponent>? store)
    {
        store = null;
        if (Exists(mindId)
            && _roles.MindHasRole<MalfAiRoleComponent>(mindId, out var role))
        {
            if (role.Value.Comp2.Store is { } pinned
                && !_deadStores.Contains(pinned)
                && !TerminatingOrDeleted(pinned)
                && TryComp<StoreComponent>(pinned, out var pinnedComp)
                && MetaData(pinned).EntityPrototype?.ID == StoreProto.Id
                && pinnedComp.AccountOwner == mindId)
            {
                store = (pinned, pinnedComp);
                return true;
            }

            role.Value.Comp2.Store = null;
        }

        var query = EntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_deadStores.Contains(uid)
                || TerminatingOrDeleted(uid)
                || MetaData(uid).EntityPrototype?.ID != StoreProto.Id
                || comp.AccountOwner != mindId)
                continue;

            PinOwnedStore(mindId, uid);
            store = (uid, comp);
            return true;
        }

        store = null;
        return false;
    }

    private void PinOwnedStore(EntityUid mindId, EntityUid store)
    {
        if (_roles.MindHasRole<MalfAiRoleComponent>(mindId, out var role))
            role.Value.Comp2.Store = store;
    }

    private void ClearPinnedStore(EntityUid mindId)
    {
        if (_roles.MindHasRole<MalfAiRoleComponent>(mindId, out var role))
            role.Value.Comp2.Store = null;
    }

    private void SetupRemoteStore(EntityUid body, EntityUid mindId, EntityUid store)
    {
        var actor = EnsureComp<MalfAiActorComponent>(body);
        if (actor.Mind != mindId)
        {
            actor.HadRemoteStore = TryComp<RemoteStoreComponent>(body, out var previous);
            actor.PreviousRemoteStore = previous?.Store;
            actor.Mind = mindId;
        }

        var remote = EnsureComp<RemoteStoreComponent>(body);
        remote.Store = store;
        actor.BoundStore = store;
        Dirty(body, remote);
        _ui.SetUi(body, StoreUiKey.Key,
            new InterfaceData("StoreBoundUserInterface", 0f, false));
    }

    private void TeardownRemoteStore(EntityUid body, EntityUid mindId)
    {
        if (!TryComp<MalfAiActorComponent>(body, out var actor)
            || actor.Mind != mindId
            || !TryComp<RemoteStoreComponent>(body, out var remote)
            || remote.Store != actor.BoundStore)
            return;

        _ui.CloseUi(body, StoreUiKey.Key);
        if (actor.HadRemoteStore)
        {
            remote.Store = actor.PreviousRemoteStore;
            Dirty(body, remote);
        }
        else
            RemComp<RemoteStoreComponent>(body);
    }

    private void DeleteOwnedStore(EntityUid mindId)
    {
        if (!TryGetOwnedStore(mindId, out var store))
            return;

        ClearPinnedStore(mindId);
        foreach (var purchased in store.Value.Comp.BoughtEntities)
            RemovePurchasedAction(purchased, mindId);
        _deadStores.Add(store.Value.Owner);
        QueueDel(store.Value.Owner);
    }

    private void RemovePurchasedAction(EntityUid action, EntityUid mindId)
    {
        if (TerminatingOrDeleted(action)
            || !TryComp<ActionComponent>(action, out var component)
            || component.Container != mindId)
            return;

        _actionContainer.RemoveAction(action, logMissing: false);
        QueueDel(action);
    }

    public void ConsumePurchasedAction(EntityUid mindId, EntityUid action)
    {
        RemovePurchasedAction(action, mindId);
    }

    public void RefundListing(EntityUid mindId, ProtoId<ListingPrototype> listingId)
    {
        if (!TryGetOwnedStore(mindId, out var store))
            return;

        ListingDataWithCostModifiers? listingData = null;
        foreach (var data in store.Value.Comp.FullListingsCatalog)
        {
            if (data.ID != listingId.Id)
                continue;

            listingData = data;
            break;
        }

        if (listingData == null || listingData.PurchaseAmount <= 0)
            return;

        if (listingData.ProductAction is { } productAction)
        {
            var purchases = store.Value.Comp.BoughtEntities;
            for (var i = purchases.Count - 1; i >= 0; i--)
            {
                var action = purchases[i];
                if (TerminatingOrDeleted(action)
                    || MetaData(action).EntityPrototype?.ID != productAction.Id)
                    continue;

                RemovePurchasedAction(action, mindId);
                purchases.RemoveAt(i);
                break;
            }
        }

        foreach (var (currency, amount) in listingData.Cost)
        {
            _store.TryAddCurrency(
                new Dictionary<string, FixedPoint2> { { currency, amount } },
                store.Value.Owner, store.Value.Comp);
        }

        if (listingData.PurchaseAmount > 0)
            listingData.PurchaseAmount--;

        if (TryComp<MindComponent>(mindId, out var mind) && mind.OwnedEntity is { } user)
            _store.UpdateUserInterface(user, store.Value.Owner, store.Value.Comp);
    }

    private bool IsModuleDisabled(ProtoId<ListingPrototype> listingId)
    {
        if (listingId == MalfAiConstants.LockdownListing)
            return !_cfg.GetCVar(CCVars220.MalfAiLockdownEnabled);

        if (listingId == MalfAiConstants.RobotFactoryListing)
            return !_cfg.GetCVar(CCVars220.MalfAiRobotFactoryEnabled);

        if (listingId == MalfAiConstants.DoomsdayListing)
            return !_cfg.GetCVar(CCVars220.MalfAiDoomsdayEnabled);

        return false;
    }

    private void OnStoreBought(ref StoreBuyFinishedEvent args)
    {
        if (!TryComp<StoreComponent>(args.StoreUid, out var store)
            || MetaData(args.StoreUid).EntityPrototype?.ID != StoreProto.Id
            || store.AccountOwner is not { } mindId)
            return;

        if (!_mind.TryGetMind(args.User, out var buyerMind, out _)
            || buyerMind != mindId
            || !_roles.MindHasRole<MalfAiRoleComponent>(mindId))
        {
            RefundListing(mindId, args.PurchasedItem.ID);
            return;
        }

        if (IsModuleDisabled(args.PurchasedItem.ID))
        {
            RefundListing(mindId, args.PurchasedItem.ID);
            _popup.PopupEntity(Loc.GetString(DenyDisabled), args.User, args.User);
            return;
        }

        if (TryGetMalfContext(args.User, out var context, out _))
        {
            if (args.PurchasedItem.ProductAction != null)
                return;

            var ev = new MalfModuleExecuteEvent(context.Value, args.PurchasedItem.ID);
            RaiseLocalEvent(ref ev);
            if (!ev.Handled)
            {
                RefundListing(mindId, args.PurchasedItem.ID);
                _popup.PopupEntity(Loc.GetString(ModuleRefunded), args.User, args.User);
            }
            return;
        }

        RefundListing(mindId, args.PurchasedItem.ID);
        _popup.PopupEntity(Loc.GetString(ModuleRefunded), args.User, args.User);
    }

    private void GrantActions(EntityUid body, EntityUid mindId)
    {
        var actor = EnsureComp<MalfAiActorComponent>(body);
        actor.Mind = mindId;
        actor.GrantedActions.RemoveAll(uid => TerminatingOrDeleted(uid));

        var knownProtos = new HashSet<string?>();
        foreach (var existingId in actor.GrantedActions)
            knownProtos.Add(MetaData(existingId).EntityPrototype?.ID);

        foreach (var proto in InnateActionPrototypes)
        {
            if (knownProtos.Contains(proto.Id))
                continue;

            EntityUid? actionId = null;
            if (_actions.AddAction(body, ref actionId, out _, proto.Id))
                actor.GrantedActions.Add(actionId.Value);
        }
    }

    private void GrantMindActions(EntityUid mindId, EntityUid body)
    {
        if (!TryComp<ActionsContainerComponent>(mindId, out var container))
            return;

        foreach (var action in container.Container.ContainedEntities)
        {
            if (TryComp<ActionComponent>(action, out var component)
                && component.AttachedEntity != body)
                _actions.GrantContainedAction(body, (mindId, container), action);
        }
    }

    public void GrantThermalVision(EntityUid body, MalfAiCameraUpgradeComponent upgrade)
    {
        if (!TryComp<MalfAiActorComponent>(body, out var actor))
            return;

        if (HasComp<ThermalVisionComponent>(body) && !actor.GrantedThermalVision)
            return;

        var thermal = EnsureComp<ThermalVisionComponent>(body);
        thermal.VisionRadius = upgrade.VisionRadius;
        thermal.HighSensitiveVisionRadius = upgrade.CloseRadius;
        thermal.State = IgnoreLightVisionOverlayState.Half;
        Dirty(body, thermal);
        actor.GrantedThermalVision = true;
    }

    private void RemoveActor(EntityUid body, EntityUid mindId)
    {
        if (!TryComp<MalfAiActorComponent>(body, out var actor) || actor.Mind != mindId)
            return;

        foreach (var action in actor.GrantedActions)
        {
            if (TerminatingOrDeleted(action) || !TryComp<ActionComponent>(action, out var component))
                continue;

            if (component.Container == body)
            {
                _actionContainer.RemoveAction(action, logMissing: false);
                QueueDel(action);
            }
            else if (component.AttachedEntity == body)
            {
                _actions.RemoveAction(body, action);
            }
        }

        if (actor.GrantedThermalVision && HasComp<ThermalVisionComponent>(body))
            RemComp<ThermalVisionComponent>(body);

        RemComp<MalfAiActorComponent>(body);
    }

    private void OnOpenStore(Entity<MalfAiActorComponent> ent, ref MalfAiOpenStoreEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Performer, out var mindId, out _)
            || !_roles.MindHasRole<MalfAiRoleComponent>(mindId))
            return;

        if (!_cfg.GetCVar(CCVars220.MalfAiEnabled))
        {
            _popup.PopupEntity(Loc.GetString(DenyDisabled), args.Performer, args.Performer);
            return;
        }

        if (!IsMalfOperational(args.Performer))
        {
            _popup.PopupEntity(Loc.GetString(DenyNotOperational), args.Performer, args.Performer);
            return;
        }

        var store = EnsureStore(mindId);

        args.Handled = true;
        OpenMalfStore(args.Performer, mindId, store.Owner);
    }

    private void OpenMalfStore(EntityUid performer, EntityUid mindId, EntityUid? store = null)
    {
        store ??= EnsureStore(mindId).Owner;

        SetupRemoteStore(performer, mindId, store.Value);
        _ui.OpenUi(performer, StoreUiKey.Key, performer);
        _store.UpdateUserInterface(performer, store.Value);
    }
}
