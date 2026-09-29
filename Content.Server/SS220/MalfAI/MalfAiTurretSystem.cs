// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Destructible.Thresholds.Triggers;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Station;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiTurretSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private SharedDeployableTurretSystem _turrets = default!;
    [Dependency] private AnchorableSystem _anchorable = default!;

    private static readonly EntProtoId TurretProto = "MalfAiDeployableTurret";

    private static readonly LocId TurretAlready = "malfai-turret-already";
    private static readonly LocId TurretDone = "malfai-turret-done";
    private static readonly LocId DeployDenied = "malfai-deploy-denied";
    private static readonly LocId DeployLimit = "malfai-deploy-limit";
    private static readonly LocId AbilityNoVision = "malfai-ability-no-vision";
    private static readonly LocId DeployDone = "malfai-deploy-done";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfModuleExecuteEvent>(OnUpgradeModule);
        SubscribeLocalEvent<DeployableTurretComponent, ComponentStartup>(OnTurretSpawned);
        SubscribeLocalEvent<MalfAiTurretBuffedComponent, GunRefreshModifiersEvent>(OnBuffRefresh);
        SubscribeLocalEvent<MalfAiTurretBuffedComponent, GunShotEvent>(OnTurretShot);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiDeployTurretEvent>(OnDeployTurret);
    }

    private void OnUpgradeModule(ref MalfModuleExecuteEvent args)
    {
        if (args.ListingId != MalfAiConstants.TurretUpgradeListing)
            return;

        args.Handled = true;
        var ctx = args.Context;

        if (HasComp<MalfAiTurretUpgradeComponent>(ctx.Station))
        {
            _malfAi.RefundListing(ctx.MindId, MalfAiConstants.TurretUpgradeListing);
            _popup.PopupEntity(Loc.GetString(TurretAlready), ctx.Performer, ctx.Performer);
            return;
        }

        EnsureComp<MalfAiTurretUpgradeComponent>(ctx.Station);

        var upgraded = 0;
        var query = EntityQueryEnumerator<DeployableTurretComponent, TransformComponent>();
        while (query.MoveNext(out var turretUid, out _, out _))
        {
            if (_station.GetOwningStation(turretUid) != ctx.Station)
                continue;

            if (ApplyBuff(turretUid))
                upgraded++;
        }

        _popup.PopupEntity(Loc.GetString(TurretDone, ("count", upgraded)), ctx.Performer, ctx.Performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(ctx.Performer):player} (Malf AI) upgraded {upgraded} station turrets.");
    }

    private void OnTurretSpawned(Entity<DeployableTurretComponent> ent, ref ComponentStartup args)
    {
        var station = _station.GetOwningStation(ent);
        if (station == null || !HasComp<MalfAiTurretUpgradeComponent>(station.Value))
            return;

        ApplyBuff(ent);
    }

    public bool ApplyBuff(EntityUid uid)
    {
        if (HasComp<MalfAiTurretBuffedComponent>(uid)
            || !TryComp<MalfAiUpgradeableTurretComponent>(uid, out var tuning))
            return false;

        if (TryComp<DestructibleComponent>(uid, out var destructible))
        {
            var hpBonus = FixedPoint2.New(tuning.HpBonus);
            foreach (var threshold in destructible.Thresholds)
            {
                if (threshold.Trigger is DamageTrigger damageTrigger)
                    damageTrigger.Damage += hpBonus;
            }
        }

        EnsureComp<MalfAiTurretBuffedComponent>(uid);

        if (TryComp<GunComponent>(uid, out var gun))
            _gun.RefreshModifiers((uid, gun));

        return true;
    }

    private void OnBuffRefresh(Entity<MalfAiTurretBuffedComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (TryComp<MalfAiUpgradeableTurretComponent>(ent, out var tuning))
            args.FireRate *= tuning.FireRateMult;
    }

    private void OnTurretShot(Entity<MalfAiTurretBuffedComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<MalfAiUpgradeableTurretComponent>(ent, out var tuning))
            return;

        var damageMult = tuning.DamageMult;
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is { } ammoUid && TryComp<ProjectileComponent>(ammoUid, out var proj))
                proj.Damage *= damageMult;
        }
    }

    private void OnDeployTurret(Entity<MalfAiActorComponent> ent, ref MalfAiDeployTurretEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;

        if (!_malfAi.TryGetMalfContext(performer, out var ctx, out var denyReason))
        {
            _popup.PopupEntity(Loc.GetString(denyReason), performer, performer);
            return;
        }

        var mindId = ctx.Value.MindId;

        var target = args.Target;
        if (_xform.GetGrid(target) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString(DeployDenied), performer, performer);
            return;
        }

        var maxTurrets = _cfg.GetCVar(CCVars220.MalfAiMaxTurrets);
        if (maxTurrets > 0 && CountDeployedTurrets() >= maxTurrets)
        {
            _popup.PopupEntity(Loc.GetString(DeployLimit, ("max", maxTurrets)), performer, performer);
            return;
        }

        if (!_map.TryGetTileRef(gridUid, gridComp, target, out var tile)
            || _turf.IsSpace(tile)
            || _turf.IsTileBlocked(tile, CollisionGroup.Impassable))
        {
            _popup.PopupEntity(Loc.GetString(DeployDenied), performer, performer);
            return;
        }

        if (!_malfAi.IsVisibleToAi(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityNoVision), performer, performer);
            return;
        }

        var coords = _map.GridTileToLocal(gridUid, gridComp, tile.GridIndices);
        if (!_anchorable.TileFree((gridUid, gridComp), tile.GridIndices,
                (int) CollisionGroup.AllMask, (int) CollisionGroup.AllMask))
        {
            _popup.PopupEntity(Loc.GetString(DeployDenied), performer, performer);
            return;
        }

        var turret = Spawn(TurretProto, coords);
        if (!Transform(turret).Anchored)
            _xform.AnchorEntity(turret);

        if (TryComp<DeployableTurretComponent>(turret, out var deployable))
            _turrets.TrySetState((turret, deployable), true);

        args.Handled = true;
        if (args.Action is { } actionId)
            _malfAi.ConsumePurchasedAction(mindId, actionId);

        _popup.PopupEntity(Loc.GetString(DeployDone), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) deployed a turret at {ToPrettyString(turret):target}.");
    }

    private int CountDeployedTurrets()
    {
        var count = 0;
        var query = EntityQueryEnumerator<MalfAiDeployedTurretComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            count++;
        }

        return count;
    }
}
