// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Server.Silicons.Laws;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Explosion.Components;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Metabolism;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.CCVars;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiRobotFactorySystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SiliconLawSystem _laws = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private AnchorableSystem _anchorable = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedPlayerManager _player = default!;

    private static readonly EntProtoId FactoryProto = "MalfAiRobotFactory";
    private static readonly EntProtoId ShellProto = "MalfAiCyborgShell";

    private static readonly ProtoId<NpcFactionPrototype> MalfFaction = "MalfAi";

    private static readonly SoundSpecifier ConvertDoneSound =
        new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

    private static readonly LocId FactoryActive = "malfai-factory-active";
    private static readonly LocId DeployDenied = "malfai-deploy-denied";

    private static readonly LocId AbilityNoVision = "malfai-ability-no-vision";
    private static readonly LocId FactoryDone = "malfai-factory-done";
    private static readonly LocId FactoryConvertDone = "malfai-factory-convert-done";

    private readonly List<EntityUid> _eat = new();
    private readonly List<EntityUid> _due = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(MetabolizerSystem));
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiRobotFactoryEvent>(OnRobotFactory);
        SubscribeLocalEvent<MalfAiFactoryComponent, EntityTerminatingEvent>(OnFactoryTerminating);
    }

    private void OnRobotFactory(Entity<MalfAiActorComponent> ent, ref MalfAiRobotFactoryEvent args)
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

        var maxFactories = _cfg.GetCVar(CCVars220.MalfAiMaxFactories);
        var factories = 0;
        var existing = EntityQueryEnumerator<MalfAiFactoryComponent, TransformComponent>();
        while (existing.MoveNext(out var existingUid, out _, out var factoryXform))
        {
            if (TerminatingOrDeleted(existingUid))
                continue;

            if (factoryXform.GridUid is not { })
                continue;

            factories++;
        }

        if (maxFactories > 0 && factories >= maxFactories)
        {
            _malfAi.RefundListing(mindId, MalfAiConstants.RobotFactoryListing);
            _popup.PopupEntity(Loc.GetString(FactoryActive, ("max", maxFactories)), performer, performer);
            return;
        }

        var target = args.Target;
        if (_xform.GetGrid(target) is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString(DeployDenied), performer, performer);
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

        args.Handled = true;

        var factory = Spawn(FactoryProto, coords);
        if (!Transform(factory).Anchored)
            _xform.AnchorEntity(factory);

        if (args.Action is { } actionId)
            _malfAi.ConsumePurchasedAction(mindId, actionId);

        _popup.PopupEntity(Loc.GetString(FactoryDone), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) deployed a robot factory at {ToPrettyString(factory):target}.");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Count<ActiveMalfAiFactoryComponent>() == 0)
            return;

        var now = _timing.CurTime;
        _eat.Clear();
        _due.Clear();
        var query = EntityQueryEnumerator<MalfAiFactoryComponent, ActiveMalfAiFactoryComponent>();
        while (query.MoveNext(out var uid, out var factory, out _))
        {
            if (!_power.IsPowered(uid))
                continue;

            if (factory.PendingBody != null)
                _eat.Add(uid);

            if (HasComp<FinishingMalfAiFactoryComponent>(uid))
            {
                if (now >= factory.FinishDue)
                    _due.Add(uid);
                continue;
            }

            if (now >= factory.ConversionDue)
                _due.Add(uid);
        }

        foreach (var uid in _eat)
        {
            if (!TryComp<MalfAiFactoryComponent>(uid, out var factory))
                continue;

            if (factory.PendingBody is not { } body)
                continue;

            factory.PendingBody = null;
            ReleaseVictimMind(factory);
            if (!TerminatingOrDeleted(body))
                QueueDel(body);
        }

        foreach (var uid in _due)
        {
            if (TerminatingOrDeleted(uid) || !TryComp<MalfAiFactoryComponent>(uid, out var factory))
                continue;

            if (!HasComp<ActiveMalfAiFactoryComponent>(uid))
                continue;

            if (factory.PendingBody != null)
                continue;

            if (!HasComp<FinishingMalfAiFactoryComponent>(uid))
            {
                factory.FinishDue = now + TimeSpan.FromSeconds(Math.Max(0f, factory.PostConversionDelay));
                EnsureComp<FinishingMalfAiFactoryComponent>(uid);
                _appearance.SetData(uid, MalfAiFactoryVisuals.Status, MalfAiFactoryStatus.Finishing);
                continue;
            }

            RemComp<FinishingMalfAiFactoryComponent>(uid);
            RemComp<ActiveMalfAiFactoryComponent>(uid);
            FinishConversion((uid, factory));
        }
    }

    private void OnFactoryTerminating(Entity<MalfAiFactoryComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.PendingBody is { } body)
        {
            ent.Comp.PendingBody = null;
            GhostVictim(ent.Comp);
            if (!TerminatingOrDeleted(body))
                QueueDel(body);
        }
        else if (HasComp<ActiveMalfAiFactoryComponent>(ent) || HasComp<FinishingMalfAiFactoryComponent>(ent))
        {
            GhostVictim(ent.Comp);
        }

        if (!HasComp<ActiveMalfAiFactoryComponent>(ent) && !HasComp<FinishingMalfAiFactoryComponent>(ent))
            return;

        RemComp<FinishingMalfAiFactoryComponent>(ent);
        RemComp<ActiveMalfAiFactoryComponent>(ent);
        _admin.Add(LogType.Action, LogImpact.Medium,
            $"Malf AI robot factory {ToPrettyString(ent):reclaimer} was destroyed mid-grind; conversion aborted.");
    }

    private void FinishConversion(Entity<MalfAiFactoryComponent> ent)
    {
        _appearance.SetData(ent, MalfAiFactoryVisuals.Status, MalfAiFactoryStatus.Idle);

        EntityUid? seatedMind = null;
        if (ent.Comp.VictimMind is { } mid
            && TryComp<MindComponent>(mid, out var victimMind)
            && victimMind.UserId != null
            && _player.TryGetSessionById(victimMind.UserId.Value, out _)
            && (victimMind.OwnedEntity == null || HasComp<GhostComponent>(victimMind.OwnedEntity.Value)))
            seatedMind = mid;
        ent.Comp.VictimMind = null;

        var borg = Spawn(ShellProto, Transform(ent).Coordinates);
        MoveOffFactoryTile(borg);

        StripTransponderSuite(borg);

        var borgFaction = EnsureComp<NpcFactionMemberComponent>(borg);
        _npcFaction.AddFaction((borg, borgFaction), MalfFaction);

        if (seatedMind is { } live)
        {
            RemComp<Content.Server.Ghost.Roles.Components.GhostRoleComponent>(borg);
            RemComp<GhostTakeoverAvailableComponent>(borg);
            _mind.TransferTo(live, borg);
        }

        _laws.InstallMalfLaw(borg);

        _audio.PlayPvs(ConvertDoneSound, ent);
        _popup.PopupEntity(Loc.GetString(FactoryConvertDone), ent);
        _admin.Add(LogType.Action, LogImpact.High,
            $"Malf AI robot factory {ToPrettyString(ent):reclaimer} finished grinding and produced {ToPrettyString(borg):target}.");
    }

    private void ReleaseVictimMind(MalfAiFactoryComponent factory)
    {
        if (factory.VictimMind is not { } mindId
            || !TryComp<MindComponent>(mindId, out var mind)
            || mind.OwnedEntity is not { } owned
            || TerminatingOrDeleted(owned)
            || HasComp<GhostComponent>(owned))
            return;

        _mind.TransferTo(mindId, null, createGhost: false, mind: mind);
    }

    private void GhostVictim(MalfAiFactoryComponent factory)
    {
        if (factory.VictimMind is not { } mindId)
            return;

        factory.VictimMind = null;
        if (!TryComp<MindComponent>(mindId, out var mind) || mind.UserId == null)
            return;

        if (mind.OwnedEntity is { } owned && !TerminatingOrDeleted(owned))
            _mind.TransferTo(mindId, null, createGhost: false, mind: mind);

        if (!_player.TryGetSessionById(mind.UserId.Value, out _))
            return;

        _ghost.SpawnGhost((mindId, mind), (EntityCoordinates?) null, canReturn: false);
    }

    private void StripTransponderSuite(EntityUid shell)
    {
        RemCompDeferred<BorgTransponderComponent>(shell);
        RemCompDeferred<TimerTriggerComponent>(shell);
        RemCompDeferred<ExplodeOnTriggerComponent>(shell);
        RemCompDeferred<ExplosiveComponent>(shell);
    }

    private void MoveOffFactoryTile(EntityUid shell)
    {
        var xform = Transform(shell);
        if (xform.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var origin = _xform.GetGridOrMapTilePosition(shell, xform);
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var tile = origin + new Vector2i(dx, dy);
                if (_map.TryGetTileRef(gridUid, grid, tile, out var tileRef)
                    && !_turf.IsSpace(tileRef)
                    && !_turf.IsTileBlocked(tileRef, CollisionGroup.MobMask))
                {
                    _xform.SetCoordinates(shell, _map.GridTileToLocal(gridUid, grid, tile));
                    return;
                }
            }
        }
    }
}
