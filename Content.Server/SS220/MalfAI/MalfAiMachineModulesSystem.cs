// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Globalization;
using Content.Server.Administration.Logs;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NPC.HTN;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Spawners.Components;
using Content.Shared.Database;
using Content.Shared.NodeContainer;
using Content.Shared.Power;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Explosion;
using Content.Shared.Explosion.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.MalfAI;
using Content.Shared.SS220.CCVars;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Server.SS220.MalfAI;

public sealed partial class MalfAiMachineModulesSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private MalfAiSystem _malfAi = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private NodeGroupSystem _nodeGroup = default!;
    [Dependency] private PowerNetConnectorSystem _powerNetConnector = default!;

    private static readonly ProtoId<HTNCompoundPrototype> HostileRootTask = "SimpleHostileCompound";
    private static readonly ProtoId<NpcFactionPrototype> HostileFaction = "MalfAi";

    private static readonly SoundSpecifier OverloadWarnSound =
        new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg");

    private static readonly EntProtoId SiphonSparksProto = "EffectSparks";
    private static readonly SoundSpecifier SiphonSound =
        new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg");
    private static readonly TimeSpan SiphonSparkInterval = TimeSpan.FromSeconds(3);
    private const string SiphonHvNode = "malfai_hv";

    private static readonly LocId AbilityNoVision = "malfai-ability-no-vision";
    private static readonly LocId MachineOverrideAlready = "malfai-machine-override-already";
    private static readonly LocId MachineOverrideLimit = "malfai-machine-override-limit";
    private static readonly LocId MachineOverrideDone = "malfai-machine-override-done";
    private static readonly LocId MachineOverloadAlready = "malfai-machine-overload-already";
    private static readonly LocId MachineOverloadPrimed = "malfai-machine-overload-primed";
    private static readonly LocId PowerSiphonAlready = "malfai-power-siphon-already";
    private static readonly LocId PowerSiphonDenied = "malfai-power-siphon-denied";
    private static readonly LocId PowerSiphonActive = "malfai-power-siphon-active";
    private static readonly LocId PowerSiphonNoHv = "malfai-power-siphon-no-hv";
    private static readonly LocId PowerSiphonDone = "malfai-power-siphon-done";
    private static readonly LocId EmagDone = "malfai-emag-done";
    private static readonly LocId EmagFailed = "malfai-emag-failed";
    private static readonly LocId EmagAlready = "malfai-emag-already";

    private static readonly ProtoId<TagPrototype> EmagImmuneTag = "EmagImmune";
    private static readonly SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    private readonly List<EntityUid> _starved = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiMachineOverrideEvent>(OnMachineOverride);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiMachineOverloadEvent>(OnMachineOverload);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiPowerSiphonEvent>(OnPowerSiphon);
        SubscribeLocalEvent<MalfAiActorComponent, MalfAiEmagEvent>(OnEmag);
        SubscribeLocalEvent<MalfAiPowerSiphonComponent, ComponentShutdown>(OnSiphonHostShutdown);
    }

    private bool HasConflictingMachineState(EntityUid target)
    {
        return HasComp<NpcFactionMemberComponent>(target)
            || HasComp<ActiveTimerTriggerComponent>(target)
            || HasComp<ExplosiveComponent>(target)
            || HasComp<PowerConsumerComponent>(target);
    }

    private bool HasConflictingOverloadState(EntityUid target)
    {
        return HasComp<TimerTriggerComponent>(target)
            || HasComp<ExplodeOnTriggerComponent>(target);
    }

    private MalfAiMachineOverloadTuningComponent ResolveOverloadTuning(EntityUid action)
    {
        if (TryComp<MalfAiMachineOverloadTuningComponent>(action, out var tuning))
            return tuning;

        return new MalfAiMachineOverloadTuningComponent();
    }

    private MalfAiPowerSiphonTuningComponent ResolveSiphonTuning(EntityUid action)
    {
        if (TryComp<MalfAiPowerSiphonTuningComponent>(action, out var tuning))
            return tuning;

        return new MalfAiPowerSiphonTuningComponent();
    }

    private EntityUid? TryGetMachineTarget(EntityUid performer, EntityUid target)
    {
        if (!_malfAi.TryGetMalfContext(performer, out var ctx))
            return null;

        if (TerminatingOrDeleted(target))
            return null;

        if (!HasComp<MalfAiTargetableComponent>(target) || HasComp<ItemComponent>(target))
            return null;

        if (!_malfAi.IsVisibleToAi(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityNoVision), performer, performer);
            return null;
        }

        return target;
    }

    private void OnMachineOverride(Entity<MalfAiActorComponent> ent, ref MalfAiMachineOverrideEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (TryGetMachineTarget(performer, args.Target) is not { } target)
            return;

        if (HasConflictingMachineState(target))
        {
            _popup.PopupEntity(Loc.GetString(MachineOverrideAlready), performer, performer);
            return;
        }

        var maxConstructs = _cfg.GetCVar(CCVars220.MalfAiMaxOverrides);
        var constructs = 0;
        var cq = EntityQueryEnumerator<MalfAiOverriddenMachineComponent>();
        while (cq.MoveNext(out var cUid, out _))
        {
            if (TerminatingOrDeleted(cUid))
                continue;

            constructs++;
        }

        if (maxConstructs > 0 && constructs >= maxConstructs)
        {
            _popup.PopupEntity(Loc.GetString(MachineOverrideLimit, ("max", maxConstructs)), performer, performer);
            return;
        }

        args.Handled = true;
        AnimateMachine((target, Transform(target)), ResolveOverrideTuning(args.Action.Owner));

        _popup.PopupEntity(Loc.GetString(MachineOverrideDone), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) overrode machine {ToPrettyString(target):target}.");
    }

    private MalfAiMachineOverrideTuningComponent ResolveOverrideTuning(EntityUid action)
    {
        if (TryComp<MalfAiMachineOverrideTuningComponent>(action, out var tuning))
            return tuning;

        return new MalfAiMachineOverrideTuningComponent();
    }

    private void AnimateMachine(Entity<TransformComponent> target, MalfAiMachineOverrideTuningComponent tuning)
    {
        var xform = target.Comp;
        if (xform.Anchored)
            _xform.Unanchor(target);

        if (TryComp<PhysicsComponent>(target, out var body))
        {
            _physics.SetBodyType(target, BodyType.KinematicController,
                body: body, xform: xform);
            _physics.SetCanCollide(target, true, body: body);
        }

        EnsureComp<InputMoverComponent>(target);

        if (!EnsureComp<HTNComponent>(target, out var htn))
            htn.RootTask = new HTNCompoundTask { Task = HostileRootTask };

        EnsureComp<MobMoverComponent>(target);
        EnsureComp<MovementSpeedModifierComponent>(target);
        EnsureComp<CombatModeComponent>(target);

        if (!EnsureComp<MeleeWeaponComponent>(target, out var weapon))
        {
            weapon.Damage = new DamageSpecifier(
                _proto.Index(tuning.DamageType), tuning.Damage);
            weapon.Range = tuning.Range;
            Dirty(target, weapon);
        }

        var faction = EnsureComp<NpcFactionMemberComponent>(target);
        _npcFaction.AddFaction((target, faction), HostileFaction);

        EnsureComp<MalfAiOverriddenMachineComponent>(target);
        EnsureComp<DamageableComponent>(target);
    }

    private void OnMachineOverload(Entity<MalfAiActorComponent> ent, ref MalfAiMachineOverloadEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (TryGetMachineTarget(performer, args.Target) is not { } target)
            return;

        if (HasConflictingMachineState(target) || HasConflictingOverloadState(target))
        {
            _popup.PopupEntity(Loc.GetString(MachineOverloadAlready), performer, performer);
            return;
        }

        args.Handled = true;

        var tuning = ResolveOverloadTuning(args.Action.Owner);
        var timer = EnsureComp<TimerTriggerComponent>(target);
        timer.Delay = TimeSpan.FromSeconds(tuning.DelaySeconds);
        timer.Examinable = false;
        timer.BeepInterval = TimeSpan.FromSeconds(tuning.BeepIntervalSeconds);
        timer.BeepSound = tuning.BeepSound;

        var explode = EnsureComp<ExplodeOnTriggerComponent>(target);
        explode.KeysIn = new HashSet<string> { "timer" };

        var explosive = EnsureComp<ExplosiveComponent>(target);
        explosive.ExplosionType = tuning.ExplosionType;
        explosive.TotalIntensity = tuning.TotalIntensity;
        explosive.IntensitySlope = tuning.IntensitySlope;
        explosive.MaxIntensity = tuning.MaxIntensity;
        explosive.CanCreateVacuum = false;
        explosive.DeleteAfterExplosion = true;

        Dirty(target, timer);
        Dirty(target, explode);

        _audio.PlayPvs(OverloadWarnSound, target);
        _popup.PopupEntity(Loc.GetString(MachineOverloadPrimed, ("seconds", (int) tuning.DelaySeconds)), performer, performer);
        _trigger.ActivateTimerTrigger(target, performer);

        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) primed machine {ToPrettyString(target):target} for overload.");
    }

    private void OnEmag(Entity<MalfAiActorComponent> ent, ref MalfAiEmagEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (!_malfAi.TryGetMalfContext(performer, out var ctx))
            return;

        var target = args.Target;
        if (TerminatingOrDeleted(target))
            return;

        if (!_malfAi.IsVisibleToAi(target))
        {
            _popup.PopupEntity(Loc.GetString(AbilityNoVision), performer, performer);
            return;
        }

        if (_tag.HasTag(target, EmagImmuneTag))
        {
            _popup.PopupEntity(Loc.GetString(EmagFailed), performer, performer);
            return;
        }

        if (_emag.CheckFlag(target, EmagType.Interaction)
            || _emag.CheckFlag(target, EmagType.Access))
        {
            _popup.PopupEntity(Loc.GetString(EmagAlready), performer, performer);
            return;
        }

        var emagged = new GotEmaggedEvent(performer, EmagType.All);
        RaiseLocalEvent(target, ref emagged);
        if (!emagged.Handled)
        {
            _popup.PopupEntity(Loc.GetString(EmagFailed), performer, performer);
            return;
        }

        args.Handled = true;

        if (!emagged.Repeatable)
        {
            var emaggedComp = EnsureComp<EmaggedComponent>(target);
            emaggedComp.EmagType |= EmagType.All;
            Dirty(target, emaggedComp);
        }

        _audio.PlayPvs(EmagSound, target);
        _popup.PopupEntity(Loc.GetString(EmagDone), performer, performer);
        _admin.Add(LogType.Emag, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) emagged {ToPrettyString(target):target}.");
    }

    private void OnPowerSiphon(Entity<MalfAiActorComponent> ent, ref MalfAiPowerSiphonEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        if (TryGetMachineTarget(performer, args.Target) is not { } target)
            return;

        if (HasComp<MalfAiPowerSiphonComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString(PowerSiphonAlready), performer, performer);
            return;
        }

        if (HasConflictingMachineState(target))
        {
            _popup.PopupEntity(Loc.GetString(PowerSiphonDenied), performer, performer);
            return;
        }

        var maxSiphons = _cfg.GetCVar(CCVars220.MalfAiMaxSiphons);
        var siphons = 0;
        var sq = EntityQueryEnumerator<MalfAiPowerSiphonComponent>();
        while (sq.MoveNext(out var sUid, out _))
        {
            if (!TerminatingOrDeleted(sUid))
                siphons++;
        }

        if (maxSiphons > 0 && siphons >= maxSiphons)
        {
            _popup.PopupEntity(Loc.GetString(PowerSiphonActive, ("max", maxSiphons)), performer, performer);
            return;
        }

        if (TryFindHvCable(target) == null)
        {
            _popup.PopupEntity(Loc.GetString(PowerSiphonNoHv), performer, performer);
            return;
        }

        args.Handled = true;

        var tuning = ResolveSiphonTuning(args.Action.Owner);
        var siphon = EnsureComp<MalfAiPowerSiphonComponent>(target);
        siphon.InitialDraw = tuning.InitialDraw;
        siphon.RampPerSecond = tuning.RampPerSecond;
        siphon.MaxDraw = tuning.MaxDraw;
        siphon.StallMinDraw = tuning.StallMinDraw;
        siphon.StallRatio = tuning.StallRatio;
        siphon.StallTimeoutSeconds = tuning.StallTimeoutSeconds;
        siphon.StallSeconds = 0f;
        siphon.AddedNodeContainer = !EnsureComp<NodeContainerComponent>(target, out var nodes);
        var nodeId = GetFreeSiphonHvNodeId(nodes);

        var mapping = new MappingDataNode();
        mapping.Add("nodeGroupID", new ValueDataNode("HVPower"));
        var hvNode = _serialization.Read<CableDeviceNode>(mapping, notNullableOverride: true);
        hvNode.Name = nodeId;
        hvNode.Initialize(target, EntityManager);
        nodes.Nodes.Add(nodeId, hvNode);
        _nodeGroup.QueueReflood(hvNode);
        siphon.AddedHvNodeId = nodeId;

        siphon.AddedPowerConsumer = !EnsureComp<PowerConsumerComponent>(target, out var consumer);
        consumer.Voltage = Voltage.High;
        consumer.NodeId = nodeId;
        consumer.DrawRate = siphon.InitialDraw;
        _powerNetConnector.BaseNetConnectorInit(consumer);

        var addedSpawner = !EnsureComp<TimedSpawnerComponent>(target, out var sparks);
        if (addedSpawner)
        {
            sparks.Prototypes = [SiphonSparksProto];
            sparks.Chance = 1f;
            sparks.IntervalSeconds = SiphonSparkInterval;
            sparks.MinimumEntitiesSpawned = 1;
            sparks.MaximumEntitiesSpawned = 1;
            sparks.NextFire = _timing.CurTime + SiphonSparkInterval;
        }

        siphon.AddedTimedSpawner = addedSpawner;

        _audio.PlayPvs(SiphonSound, target);
        _popup.PopupEntity(Loc.GetString(PowerSiphonDone), performer, performer);
        _admin.Add(LogType.Action, LogImpact.High,
            $"{ToPrettyString(performer):player} (Malf AI) siphoned HV power through {ToPrettyString(target):target}.");
    }

    private static string GetFreeSiphonHvNodeId(NodeContainerComponent nodes)
    {
        var nodeId = SiphonHvNode;
        for (var suffix = 1; nodes.Nodes.ContainsKey(nodeId); suffix++)
        {
            nodeId = $"{SiphonHvNode}_{suffix.ToString(CultureInfo.InvariantCulture)}";
        }

        return nodeId;
    }

    private EntityUid? TryFindHvCable(EntityUid machine)
    {
        var xform = Transform(machine);
        if (!xform.Anchored || xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return null;

        var tile = _map.LocalToTile(gridUid, grid, xform.Coordinates);
        foreach (var ent in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            if (!TryComp<CableComponent>(ent, out var cable)
                || cable.CableType != CableType.HighVoltage
                || !TryComp<NodeContainerComponent>(ent, out var nodes))
            {
                continue;
            }

            foreach (var node in nodes.Nodes.Values)
            {
                if (node is CableNode && node.NodeGroupID == NodeGroupID.HVPower)
                    return ent;
            }
        }

        return null;

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Count<MalfAiPowerSiphonComponent>() == 0)
            return;

        _starved.Clear();
        var query = EntityQueryEnumerator<MalfAiPowerSiphonComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var uid, out var siphon, out var consumer))
        {
            if (consumer.DrawRate < siphon.MaxDraw)
                consumer.DrawRate = Math.Min(siphon.MaxDraw, consumer.DrawRate + siphon.RampPerSecond * frameTime);

            if (siphon.StallTimeoutSeconds > 0f
                && consumer.DrawRate >= siphon.StallMinDraw
                && consumer.ReceivedPower < consumer.DrawRate * siphon.StallRatio)
            {
                siphon.StallSeconds += frameTime;
                if (siphon.StallSeconds >= siphon.StallTimeoutSeconds && !_starved.Contains(uid))
                    _starved.Add(uid);

                continue;
            }

            siphon.StallSeconds = 0f;
        }

        foreach (var uid in _starved)
        {
            if (TerminatingOrDeleted(uid) || !TryComp<PowerConsumerComponent>(uid, out var consumer))
                continue;

            _admin.Add(LogType.Action, LogImpact.Medium,
                $"Malf AI power siphon on {ToPrettyString(uid):target} starved " +
                $"(demanded {consumer.DrawRate:F0} W, received {consumer.ReceivedPower:F0} W) and was removed.");
            RemComp<MalfAiPowerSiphonComponent>(uid);
        }
    }

    private void OnSiphonHostShutdown(Entity<MalfAiPowerSiphonComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.AddedPowerConsumer && HasComp<PowerConsumerComponent>(ent))
            RemCompDeferred<PowerConsumerComponent>(ent);

        if (ent.Comp.AddedHvNodeId is { } nodeId
            && TryComp<NodeContainerComponent>(ent, out var nodes)
            && nodes.Nodes.Remove(nodeId, out var hvNode))
        {
            _nodeGroup.QueueNodeRemove(hvNode);
            hvNode.Deleting = true;
        }

        if (ent.Comp.AddedNodeContainer)
            RemCompDeferred<NodeContainerComponent>(ent);

        if (ent.Comp.AddedTimedSpawner && HasComp<TimedSpawnerComponent>(ent))
            RemCompDeferred<TimedSpawnerComponent>(ent);
    }
}
