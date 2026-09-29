// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Pair;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Nodes;
using Content.Server.Spawners.Components;
using Content.Server.SS220.MalfAI;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Actions.Components;
using Content.Shared.Emag.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Station.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiMachineModulesTest : GameTest
{
    private EntityUid _overloadMachine;
    private EntityUid _factoryBody;
    private EntityUid _factoryStore;
    private EntityUid _factoryEnt;
    private EntityUid _transferMindId;

    private static void MakeStation(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        entMan.EnsureComponent<StationDataComponent>(map.MapUid);
        entMan.System<Content.Server.Station.Systems.StationSystem>()
            .AddGridToStation(map.MapUid, map.GridCoords.EntityId);
    }

    private static EntityUid SpawnRoleBody(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var mindSys = entMan.System<SharedMindSystem>();
        var roleSys = entMan.System<SharedRoleSystem>();

        var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
        var brain = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
        var slotSys = entMan.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>();
        var slots = entMan.GetComponent<Content.Shared.Containers.ItemSlots.ItemSlotsComponent>(core);
        slotSys.TryInsert(core, "station_ai_mind_slot", brain, null, slots);

        var mind = mindSys.CreateMind(null);
        mindSys.TransferTo(mind, brain, mind: mind);
        roleSys.MindAddRole(mind, "MindRoleMalfAi");
        return brain;
    }

    private static EntityUid FindStore(TestPair pair, EntityUid mind)
    {
        var entMan = pair.Server.EntMan;
        var query = entMan.AllEntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountOwner == mind && comp.CurrencyWhitelist.Contains(MalfAiConstants.CpuCurrency))
                return uid;
        }

        Assert.Fail("Malf store not found.");
        return EntityUid.Invalid;
    }

    private static List<EntityUid> FindFactories(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var factories = new List<EntityUid>();
        var query = entMan.AllEntityQueryEnumerator<MalfAiFactoryComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (!entMan.Deleted(uid) && xform.GridUid == map.GridCoords.EntityId)
                factories.Add(uid);
        }

        return factories;
    }

    private static void TopUp(TestPair pair, EntityUid store, int amount)
    {
        var entMan = pair.Server.EntMan;
        entMan.System<SharedStoreSystem>().TryAddCurrency(
            new System.Collections.Generic.Dictionary<string, FixedPoint2> { { MalfAiConstants.CpuCurrency, amount } },
            store, entMan.GetComponent<StoreComponent>(store));
    }

    private static void Buy(TestPair pair, EntityUid store, EntityUid buyer, string listing)
    {
        var msg = new StoreBuyListingMessage(listing, null) { Actor = buyer };
        pair.Server.EntMan.EventBus.RaiseLocalEvent(store, msg);
    }

    private static EntityUid GrantedAction(TestPair pair, EntityUid body, string actionProto)
    {
        var entMan = pair.Server.EntMan;
        EntityUid? granted = null;
        if (entMan.TryGetComponent<ActionsComponent>(body, out var actions))
        {
            foreach (var actionId in actions.Actions)
            {
                if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == actionProto)
                    granted = actionId;
            }
        }

        Assert.That(granted, Is.Not.Null, $"{actionProto} was not granted on purchase.");
        return granted!.Value;
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfMachineOverrideAnimates()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var machine = entMan.SpawnEntity("Autolathe", map.GridCoords);

            Buy(pair, store, body, MalfAiConstants.MachineOverrideListing);
            var action = GrantedAction(pair, body, "ActionMalfAiMachineOverride");

            bool Override(EntityUid performer, EntityUid target)
            {
                var ev = new MalfAiMachineOverrideEvent { Performer = performer, Target = target };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled;
            }

            var pen = entMan.SpawnEntity("Pen", map.GridCoords);
            Assert.That(Override(body, pen), Is.False,
                "Machine Override fired on an item.");
            Assert.That(entMan.HasComponent<Content.Shared.NPC.Components.NpcFactionMemberComponent>(pen), Is.False,
                "Machine Override animated an item.");

            var actionsSys = entMan.System<Content.Shared.Actions.SharedActionsSystem>();
            var validateTarget = entMan.GetComponent<Content.Shared.Actions.Components.EntityTargetActionComponent>(action);
            Assert.That(actionsSys.ValidateEntityTarget(body, machine, (action, validateTarget)), Is.True,
                "Machine Override fails engine validation in live game.");
            Assert.That(actionsSys.ValidateEntityTarget(body, pen, (action, validateTarget)), Is.False,
                "Machine Override passes engine validation for an item.");

            var locker = entMan.SpawnEntity("LockerSteel", map.GridCoords);
            Assert.That(actionsSys.ValidateEntityTarget(body, locker, (action, validateTarget)), Is.False,
                "Machine Override passes engine validation on a locker.");

            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            var actionComp = entMan.GetComponent<ActionComponent>(action);
            actionsSys.PerformAction((body, actionsComp), (action, actionComp),
                new MalfAiMachineOverrideEvent { Performer = body, Target = machine }, predicted: false);

            Assert.That(entMan.HasComponent<Content.Server.NPC.HTN.HTNComponent>(machine), Is.True,
                "Machine Override did not animate the machine.");
            var faction = entMan.GetComponent<Content.Shared.NPC.Components.NpcFactionMemberComponent>(machine);
            Assert.That(faction.Factions, Does.Contain("MalfAi"),
                "Overridden machine is not MalfAi.");
            Assert.That(entMan.HasComponent<Content.Shared.Weapons.Melee.MeleeWeaponComponent>(machine), Is.True,
                "Overridden machine has no melee weapon.");

            var machineXform = entMan.GetComponent<TransformComponent>(machine);
            Assert.That(machineXform.Anchored, Is.False,
                "Overridden machine stayed anchored and cannot move.");
            if (entMan.TryGetComponent<Robust.Shared.Physics.Components.PhysicsComponent>(machine, out var machineBody))
                Assert.That(machineBody.BodyType, Is.Not.EqualTo(Robust.Shared.Physics.BodyType.Static),
                    "Overridden machine stayed a static body and cannot move.");
            Assert.That(entMan.HasComponent<Content.Shared.Movement.Components.InputMoverComponent>(machine), Is.True,
                "Overridden machine got no InputMover: MobMover never receives intents.");

            var cooldown = entMan.GetComponent<ActionComponent>(action).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Machine Override did not start its cooldown.");

            Assert.That(actionsSys.ValidateEntityTarget(body, machine, (action, validateTarget)), Is.False,
                "Machine Override still validates an already-animated machine.");

            Assert.That(Override(body, machine), Is.False,
                "Machine Override fired twice on the same target.");

            Buy(pair, store, body, MalfAiConstants.MachineOverloadListing);
            var overloadEv = new MalfAiMachineOverloadEvent { Performer = body, Target = machine };
            entMan.EventBus.RaiseLocalEvent(body, overloadEv);
            Assert.That(overloadEv.Handled, Is.False,
                "Overload stacked onto an overridden machine.");
            Assert.That(entMan.HasComponent<ActiveTimerTriggerComponent>(machine), Is.False,
                "Overload primed an overridden machine.");

            Assert.That(entMan.HasComponent<MalfAiOverriddenMachineComponent>(machine), Is.True,
                "Overridden machine carries no cap marker.");

            for (var i = 0; i < 4; i++)
            {
                var extra = entMan.SpawnEntity("Autolathe", map.GridCoords);
                Assert.That(Override(body, extra), Is.True,
                    $"Override refused construct {i + 2} below the cap.");
            }

            var overCap = entMan.SpawnEntity("Autolathe", map.GridCoords);
            Assert.That(Override(body, overCap), Is.False,
                "Override animated past the station cap.");
            Assert.That(entMan.HasComponent<MalfAiOverriddenMachineComponent>(overCap), Is.False,
                "Over-cap machine was animated.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfMachineOverloadExplodes()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var dormantTimerMachine = entMan.SpawnEntity("Autolathe", map.GridCoords);
            var dormantTimer = entMan.AddComponent<TimerTriggerComponent>(dormantTimerMachine);
            dormantTimer.Delay = TimeSpan.FromSeconds(17);
            dormantTimer.KeyOut = "custom-timer";
            dormantTimer.KeysIn = ["custom-trigger"];
            var dormantTimerEv = new MalfAiMachineOverloadEvent { Performer = body, Target = dormantTimerMachine };
            entMan.EventBus.RaiseLocalEvent(body, dormantTimerEv);
            Assert.That(dormantTimerEv.Handled, Is.False,
                "Machine Overload reused a dormant custom timer trigger.");
            Assert.That(dormantTimer.Delay, Is.EqualTo(TimeSpan.FromSeconds(17)));
            Assert.That(dormantTimer.KeyOut, Is.EqualTo("custom-timer"));
            Assert.That(dormantTimer.KeysIn, Is.EqualTo(new[] { "custom-trigger" }));
            Assert.That(entMan.HasComponent<ActiveTimerTriggerComponent>(dormantTimerMachine), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.Explosion.Components.ExplosiveComponent>(dormantTimerMachine), Is.False);

            var explodeTriggerMachine = entMan.SpawnEntity("Autolathe", map.GridCoords);
            var explodeTrigger = entMan.AddComponent<ExplodeOnTriggerComponent>(explodeTriggerMachine);
            explodeTrigger.KeysIn = ["custom-explosion"];
            explodeTrigger.TargetUser = true;
            var explodeTriggerEv = new MalfAiMachineOverloadEvent { Performer = body, Target = explodeTriggerMachine };
            entMan.EventBus.RaiseLocalEvent(body, explodeTriggerEv);
            Assert.That(explodeTriggerEv.Handled, Is.False,
                "Machine Overload reused a custom explode-on-trigger effect.");
            Assert.That(explodeTrigger.KeysIn, Is.EquivalentTo(new[] { "custom-explosion" }));
            Assert.That(explodeTrigger.TargetUser, Is.True);
            Assert.That(entMan.HasComponent<TimerTriggerComponent>(explodeTriggerMachine), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.Explosion.Components.ExplosiveComponent>(explodeTriggerMachine), Is.False);

            var machine = entMan.SpawnEntity("Autolathe", map.GridCoords);
            _overloadMachine = machine;

            Buy(pair, store, body, MalfAiConstants.MachineOverloadListing);
            var action = GrantedAction(pair, body, "ActionMalfAiMachineOverload");

            var actionsSys = entMan.System<Content.Shared.Actions.SharedActionsSystem>();
            var validateTarget = entMan.GetComponent<Content.Shared.Actions.Components.EntityTargetActionComponent>(action);
            Assert.That(actionsSys.ValidateEntityTarget(body, machine, (action, validateTarget)), Is.True,
                "Machine Overload fails engine validation in live game.");
            var locker = entMan.SpawnEntity("LockerSteel", map.GridCoords);
            Assert.That(actionsSys.ValidateEntityTarget(body, locker, (action, validateTarget)), Is.False,
                "Machine Overload passes engine validation on a locker.");

            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            var actionComp = entMan.GetComponent<ActionComponent>(action);
            var tuning = entMan.GetComponent<MalfAiMachineOverloadTuningComponent>(action);
            Assert.That(tuning.DelaySeconds, Is.EqualTo(300f),
                "Machine Overload default fuse is not 5 minutes.");
            tuning.DelaySeconds = 2f;
            actionsSys.PerformAction((body, actionsComp), (action, actionComp),
                new MalfAiMachineOverloadEvent { Performer = body, Target = machine }, predicted: false);

            Assert.That(entMan.HasComponent<ActiveTimerTriggerComponent>(machine), Is.True,
                "Machine Overload did not prime the machine.");
            var timer = entMan.GetComponent<TimerTriggerComponent>(machine);
            Assert.That(timer.Delay, Is.EqualTo(TimeSpan.FromSeconds(2)));

            var cooldown = entMan.GetComponent<ActionComponent>(action).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Machine Overload did not start its cooldown.");

            Assert.That(actionsSys.ValidateEntityTarget(body, machine, (action, validateTarget)), Is.False,
                "Machine Overload still validates an already-primed machine.");

            var ev = new MalfAiMachineOverloadEvent { Performer = body, Target = machine };
            entMan.EventBus.RaiseLocalEvent(body, ev);
            Assert.That(ev.Handled, Is.False,
                "Machine Overload double-primed an already-rigged machine.");

            Buy(pair, store, body, MalfAiConstants.MachineOverrideListing);
            var overrideEv = new MalfAiMachineOverrideEvent { Performer = body, Target = machine };
            entMan.EventBus.RaiseLocalEvent(body, overrideEv);
            Assert.That(overrideEv.Handled, Is.False,
                "Override stacked onto an overloaded machine.");
            Assert.That(entMan.HasComponent<Content.Shared.NPC.Components.NpcFactionMemberComponent>(machine), Is.False,
                "Override animated an overloaded machine.");
        });

        await server.WaitRunTicks(220);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var machine = _overloadMachine;

            Assert.That(entMan.HasComponent<ActiveTimerTriggerComponent>(machine), Is.False,
                "The overload timer never fired.");

            Assert.That(entMan.Deleted(machine) || !entMan.EntityExists(machine), Is.True,
                "The overload timer fired but the machine did not explode (key wiring broken).");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRobotFactoryDeploysAndGates()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();
        EntityUid corpse = default;
        EntityUid feeder = default;
        EntityUid fodder = default;
        EntityUid crewBorg = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action = GrantedAction(pair, body, "ActionMalfAiRobotFactory");

            void Deploy(EntityUid performer, EntityCoordinates target)
            {
                var actComp = entMan.GetComponent<Content.Shared.Actions.Components.ActionComponent>(action);
                var ev = new MalfAiRobotFactoryEvent
                {
                    Performer = performer,
                    Target = target,
                    Action = new Entity<Content.Shared.Actions.Components.ActionComponent>(action, actComp)
                };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
            }

            var mapSys = entMan.System<SharedMapSystem>();
            var tileMan = server.ResolveDependency<Robust.Shared.Map.ITileDefinitionManager>();
            var floorTile = new Robust.Shared.Map.Tile(tileMan["FloorSteel"].TileId);
            var gridEnt = new Entity<Robust.Shared.Map.Components.MapGridComponent>(
                map.GridCoords.EntityId,
                entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
            var originTile = mapSys.LocalToTile(gridEnt.Owner, gridEnt.Comp,
                entMan.GetComponent<TransformComponent>(map.GridCoords.EntityId).Coordinates);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    mapSys.SetTile(gridEnt, originTile + new Robust.Shared.Maths.Vector2i(dx, dy), floorTile);
                }
            }

            var homeTarget = map.GridCoords.Offset(new System.Numerics.Vector2(1, 0));
            Deploy(body, homeTarget);
            var factories = FindFactories(pair, map);
            Assert.That(factories.Count, Is.EqualTo(1),
                "Factory was not deployed on the home station.");
            var factory = factories[0];
            entMan.System<PowerReceiverSystem>().SetNeedsPower(factory, false);
            Assert.That(entMan.GetComponent<TransformComponent>(factory).Anchored, Is.True);

            entMan.GetComponent<MalfAiFactoryComponent>(factory).ConversionTime = 1f;
            entMan.GetComponent<MalfAiFactoryComponent>(factory).InsertionDelayPerMass = 0f;

            var xformSys = entMan.System<Robust.Shared.GameObjects.SharedTransformSystem>();
            xformSys.Unanchor(factory); // simulate unbolt without the verb
            var ev2 = new MalfAiRobotFactoryEvent { Performer = body, Target = homeTarget };
            entMan.EventBus.RaiseLocalEvent(body, ev2);
            xformSys.AnchorEntity(factory);
            Assert.That(FindFactories(pair, map).Count, Is.EqualTo(1),
                "A second factory was deployed while one stands (unanchored dup).");

            entMan.GetComponent<StoreComponent>(store).Balance.TryGetValue(MalfAiConstants.CpuCurrency, out var balBefore);
            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action2 = GrantedAction(pair, body, "ActionMalfAiRobotFactory");
            var evDeny = new MalfAiRobotFactoryEvent { Performer = body, Target = homeTarget };
            entMan.EventBus.RaiseLocalEvent(body, evDeny);
            entMan.GetComponent<StoreComponent>(store).Balance.TryGetValue(MalfAiConstants.CpuCurrency, out var balAfter);
            Assert.That(balAfter, Is.EqualTo(balBefore), "Re-buy with an active factory burned CPU instead of refunding.");
            Assert.That(FindFactories(pair, map).Count, Is.EqualTo(1));

            var spentActions = entMan.GetComponent<ActionsComponent>(body).Actions;
            Assert.That(spentActions, Does.Not.Contain(action),
                "Factory deploy action was not spent on placement.");

            var mindSys = entMan.System<Content.Server.Mind.MindSystem>();
            corpse = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.2f, 0f)));
            var corpseMind = mindSys.CreateMind(null);
            mindSys.TransferTo(corpseMind, corpse, mind: corpseMind);
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(corpse, Content.Shared.Mobs.MobState.Dead);

            feeder = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.1f, 0f)));

            var living = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var liveEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, living);
            entMan.EventBus.RaiseLocalEvent(factory, ref liveEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "Factory accepted a living body.");
            Assert.That(entMan.Deleted(living), Is.False, "Factory ground up a living body.");

            var mouse = entMan.SpawnEntity("MobMouse", map.GridCoords);
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(mouse, Content.Shared.Mobs.MobState.Dead);
            var mouseEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, mouse);
            entMan.EventBus.RaiseLocalEvent(factory, ref mouseEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "Factory accepted a non-humanoid body.");
            Assert.That(entMan.Deleted(mouse), Is.False, "Factory ground up a non-humanoid body.");

            fodder = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.2f, 0f)));
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(fodder, Content.Shared.Mobs.MobState.Dead);
            var aiEv = new Content.Shared.DragDrop.DragDropTargetEvent(body, fodder);
            entMan.EventBus.RaiseLocalEvent(factory, ref aiEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "Handless AI fed the factory.");
            Assert.That(entMan.Deleted(fodder), Is.False, "Handless AI ground up a corpse.");

            crewBorg = entMan.SpawnEntity("BorgChassisGeneric", map.GridCoords.Offset(new System.Numerics.Vector2(1.1f, 0f)));

            _factoryBody = body;
            _factoryStore = store;
            _factoryEnt = factory;
        });

        await server.WaitRunTicks(5);
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var factory = _factoryEnt;
            var crewPreview = new Content.Shared.DragDrop.CanDropTargetEvent(crewBorg, fodder);
            entMan.EventBus.RaiseLocalEvent(factory, ref crewPreview);
            Assert.That(crewPreview.CanDrop, Is.True, "A crew borg could not feed the factory.");

            var dropEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref dropEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.True,
                "Factory did not start grinding an inserted corpse.");
            Assert.That(entMan.GetComponent<TransformComponent>(corpse).GridUid, Is.Null,
                "Inserted corpse was not removed from the floor.");

            var secondCorpse = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(secondCorpse, Content.Shared.Mobs.MobState.Dead);
            var busyEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, secondCorpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref busyEv);
            Assert.That(entMan.Deleted(secondCorpse), Is.False,
                "Factory accepted a second corpse while busy.");
        });

        await server.WaitRunTicks(2000);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            var borgs = new List<EntityUid>();
            var chassisQuery = entMan.AllEntityQueryEnumerator<Content.Shared.Silicons.Borgs.Components.BorgChassisComponent>();
            while (chassisQuery.MoveNext(out var chassis, out _))
            {
                if (entMan.GetComponent<TransformComponent>(chassis).GridUid == map.GridCoords.EntityId
                    && entMan.GetComponent<MetaDataComponent>(chassis).EntityPrototype?.ID == "MalfAiCyborgShell")
                    borgs.Add(chassis);
            }

            Assert.That(borgs.Count, Is.EqualTo(1),
                "The factory did not produce exactly one cyborg.");
            var borg = borgs[0];

            var mindSys = entMan.System<SharedMindSystem>();
            Assert.That(mindSys.TryGetMind(borg, out _, out _), Is.False,
                "Sessionless corpse mind transferred: the ghost role is now bricked.");
            Assert.That(entMan.HasComponent<GhostTakeoverAvailableComponent>(borg), Is.True,
                "Shell ghost takeover is not available.");
            Assert.That(entMan.GetComponent<GhostRoleComponent>(borg).Taken, Is.False,
                "Shell ghost role is taken with no mind inside.");

            Assert.That(entMan.HasComponent<SiliconLawProviderComponent>(borg), Is.True);
            var provider = entMan.GetComponent<SiliconLawProviderComponent>(borg);
            var laws = provider.Lawset?.Laws;
            Assert.That(laws, Is.Not.Null.And.Count.GreaterThan(0));
            Assert.That(laws![0].Order == 0, Is.True,
                "Malf zero law is not on top of the shell lawset.");

            var containerSys = entMan.System<Robust.Shared.Containers.SharedContainerSystem>();
            var moduleContainer = containerSys.GetContainer(borg, "borg_module");
            Assert.That(moduleContainer.ContainedEntities.Count, Is.EqualTo(3),
                "The cyborg was assembled without its tool modules.");

            var borgFaction = entMan.GetComponent<Content.Shared.NPC.Components.NpcFactionMemberComponent>(borg);
            Assert.That(borgFaction.Factions,
                Is.EquivalentTo(new[] { new ProtoId<Content.Shared.NPC.Prototypes.NpcFactionPrototype>("MalfAi") }),
                "The converted cyborg must belong only to MalfAi.");

            Assert.That(entMan.HasComponent<Content.Shared.Silicons.Borgs.Components.BorgTransponderComponent>(borg), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.Trigger.Components.TimerTriggerComponent>(borg), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.Explosion.Components.ExplosiveComponent>(borg), Is.False);

            var borgPos = entMan.GetComponent<TransformComponent>(borg).Coordinates;
            var factoryPos = entMan.GetComponent<TransformComponent>(_factoryEnt).Coordinates;
            Assert.That(borgPos.Equals(factoryPos), Is.False,
                "The cyborg spawned inside the factory tile.");

            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(_factoryEnt), Is.False,
                "The factory stayed busy after finishing a grind.");

            Assert.That(entMan.HasComponent<MalfAiFactoryComponent>(_factoryEnt), Is.True);
            entMan.DeleteEntity(_factoryEnt);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var body = _factoryBody;
            var store = _factoryStore;
            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            GrantedAction(pair, body, "ActionMalfAiRobotFactory");
            var reEv = new MalfAiRobotFactoryEvent
            {
                Performer = body,
                Target = map.GridCoords.Offset(new System.Numerics.Vector2(1, 0)),
            };
            entMan.EventBus.RaiseLocalEvent(body, reEv);
            Assert.That(FindFactories(pair, map).Count, Is.EqualTo(1),
                "Factory could not be re-deployed after destruction.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfFactoryDeniedOnOccupiedTile()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var mapSys = entMan.System<SharedMapSystem>();
            var tileMan = server.ResolveDependency<Robust.Shared.Map.ITileDefinitionManager>();
            var floorTile = new Robust.Shared.Map.Tile(tileMan["FloorSteel"].TileId);
            var gridEnt = new Entity<Robust.Shared.Map.Components.MapGridComponent>(
                map.GridCoords.EntityId,
                entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
            var originTile = mapSys.LocalToTile(gridEnt.Owner, gridEnt.Comp,
                entMan.GetComponent<TransformComponent>(map.GridCoords.EntityId).Coordinates);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    mapSys.SetTile(gridEnt, originTile + new Robust.Shared.Maths.Vector2i(dx, dy), floorTile);
                }
            }

            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action = GrantedAction(pair, body, "ActionMalfAiRobotFactory");

            var target = map.GridCoords.Offset(new System.Numerics.Vector2(1, 0));

            EntityUid Deploy(EntityUid deployAction)
            {
                var actComp = entMan.GetComponent<Content.Shared.Actions.Components.ActionComponent>(deployAction);
                var deployEv = new MalfAiRobotFactoryEvent
                {
                    Performer = body,
                    Target = target,
                    Action = new Entity<Content.Shared.Actions.Components.ActionComponent>(deployAction, actComp)
                };
                entMan.EventBus.RaiseLocalEvent(body, deployEv);
                return deployEv.Handled ? deployAction : EntityUid.Invalid;
            }

            Assert.That(Deploy(action), Is.Not.EqualTo(EntityUid.Invalid),
                "First factory deploy was denied on a free tile.");
            Assert.That(FindFactories(pair, map).Count, Is.EqualTo(1),
                "First factory was not deployed.");

            // The standing factory is anchored but collides off the Impassable layer,
            // so the second deploy must be denied by the TileFree check itself.
            server.ResolveDependency<Robust.Shared.Configuration.IConfigurationManager>()
                .SetCVar(Content.Shared.SS220.CCVars.CCVars220.MalfAiMaxFactories, 2);
            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action2 = GrantedAction(pair, body, "ActionMalfAiRobotFactory");

            Assert.That(Deploy(action2), Is.EqualTo(EntityUid.Invalid),
                "Factory deploy on an occupied tile was marked handled (purchase lost).");
            Assert.That(FindFactories(pair, map).Count, Is.EqualTo(1),
                "Factory was deployed onto an occupied tile.");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRobotFactoryInsertionRequiresHands(bool interrupt)
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid factory = default;
        EntityUid corpse = default;
        EntityUid feeder = default;
        EntityUid body = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);
            var mapSys = entMan.System<SharedMapSystem>();
            var grid = new Entity<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId,
                entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
            var tileMan = server.ResolveDependency<ITileDefinitionManager>();
            var floor = new Tile(tileMan["FloorSteel"].TileId);
            var origin = mapSys.LocalToTile(grid.Owner, grid.Comp, map.GridCoords);
            for (var x = -1; x <= 7; x++)
            {
                for (var y = -1; y <= 1; y++)
                    mapSys.SetTile(grid, origin + new Robust.Shared.Maths.Vector2i(x, y), floor);
            }

            body = SpawnRoleBody(pair, map);
            var factoryCoords = map.GridCoords.Offset(new System.Numerics.Vector2(3f, 0f));
            factory = entMan.SpawnEntity("MalfAiRobotFactory", factoryCoords);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(factory, false);
            feeder = entMan.SpawnEntity("MobHuman", factoryCoords.Offset(new System.Numerics.Vector2(1.1f, 0f)));
            corpse = entMan.SpawnEntity("MobHuman", factoryCoords.Offset(new System.Numerics.Vector2(1.2f, 0f)));
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(corpse, Content.Shared.Mobs.MobState.Dead);
            var comp = entMan.GetComponent<MalfAiFactoryComponent>(factory);
            Assert.That(comp.InsertionDelayPerMass, Is.GreaterThan(0f));
            Assert.That(entMan.GetComponent<Robust.Shared.Physics.Components.PhysicsComponent>(corpse).FixturesMass,
                Is.GreaterThan(0f));
            Assert.That(entMan.HasComponent<Content.Shared.Hands.Components.HandsComponent>(body), Is.False);
        });
        await server.WaitRunTicks(5);
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<Content.Shared.Hands.Components.HandsComponent>(feeder).Count,
                Is.GreaterThan(0), "The feeder spawned without usable hands.");

            var aiDrop = new Content.Shared.DragDrop.DragDropTargetEvent(body, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref aiDrop);
            Assert.That(entMan.HasComponent<Content.Shared.DoAfter.ActiveDoAfterComponent>(body), Is.False,
                "The handless AI started a factory insertion.");
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "The handless AI fed the factory.");

            var drop = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref drop);
            Assert.That(entMan.HasComponent<Content.Shared.DoAfter.ActiveDoAfterComponent>(feeder), Is.True,
                "A feeder with hands did not start a nonzero insertion DoAfter.");
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "The insertion delay was bypassed.");
        });

        if (interrupt)
        {
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                entMan.System<SharedTransformSystem>().SetCoordinates(corpse,
                    map.GridCoords.Offset(new System.Numerics.Vector2(7f, 0f)));
            });
            await server.WaitRunTicks(5);
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                entMan.System<SharedTransformSystem>().SetCoordinates(corpse,
                    map.GridCoords.Offset(new System.Numerics.Vector2(4.5f, 0f)));
            });
        }

        await server.WaitRunTicks(1200);
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.False,
                "The factory stayed active after insertion was cancelled or conversion completed.");
            Assert.That(entMan.Deleted(corpse), Is.EqualTo(!interrupt));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRobotFactoryDestroyAborts()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid factory = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);
            var mapSys = entMan.System<SharedMapSystem>();
            var grid = new Entity<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId,
                entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
            var tileMan = server.ResolveDependency<ITileDefinitionManager>();
            var floor = new Tile(tileMan["FloorSteel"].TileId);
            var origin = mapSys.LocalToTile(grid.Owner, grid.Comp, map.GridCoords);
            for (var x = -1; x <= 2; x++)
            {
                for (var y = -1; y <= 1; y++)
                    mapSys.SetTile(grid, origin + new Robust.Shared.Maths.Vector2i(x, y), floor);
            }

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action = GrantedAction(pair, body, "ActionMalfAiRobotFactory");
            var actComp = entMan.GetComponent<ActionComponent>(action);
            var deployEv = new MalfAiRobotFactoryEvent
            {
                Performer = body,
                Target = map.GridCoords.Offset(new System.Numerics.Vector2(1, 0)),
                Action = new Entity<ActionComponent>(action, actComp)
            };
            entMan.EventBus.RaiseLocalEvent(body, deployEv);

            var factories = FindFactories(pair, map);
            Assert.That(factories.Count, Is.EqualTo(1), "Factory was not deployed on the test grid.");
            factory = factories[0];
            entMan.System<PowerReceiverSystem>().SetNeedsPower(factory, false);
            entMan.GetComponent<MalfAiFactoryComponent>(factory).ConversionTime = 1f;
            entMan.GetComponent<MalfAiFactoryComponent>(factory).InsertionDelayPerMass = 0f;
        });
        await server.WaitRunTicks(5);
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            var feeder = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.1f, 0f)));
            var corpse = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.2f, 0f)));
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(corpse, Content.Shared.Mobs.MobState.Dead);
            var dropEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref dropEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.True,
                "Factory did not start grinding.");

            entMan.DeleteEntity(factory);
        });

        await server.WaitRunTicks(200);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var borgs = 0;
            var chassisQuery = entMan.AllEntityQueryEnumerator<Content.Shared.Silicons.Borgs.Components.BorgChassisComponent>();
            while (chassisQuery.MoveNext(out var chassis, out _))
            {
                if (entMan.GetComponent<TransformComponent>(chassis).GridUid == map.GridCoords.EntityId)
                    borgs++;
            }

            Assert.That(borgs, Is.EqualTo(0),
                "Destroying the factory mid-grind still spawned a cyborg.");
        });
    }

    [Test]
    public async Task MalfRobotFactoryTransfersLiveMind()
    {
        var pair = Pair;
        var server = pair.Server;
        EntityUid producedBorg = default;
        EntityUid previewCorpse = default;
        EntityUid previewCrewBorg = default;
        EntityUid corpse = default;
        EntityUid feeder = default;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.RobotFactoryListing);
            var action = GrantedAction(pair, body, "ActionMalfAiRobotFactory");
            var actComp = entMan.GetComponent<ActionComponent>(action);

            var mapSys = entMan.System<SharedMapSystem>();
            var tileMan = server.ResolveDependency<Robust.Shared.Map.ITileDefinitionManager>();
            var floorTile = new Robust.Shared.Map.Tile(tileMan["FloorSteel"].TileId);
            var gridEnt = new Entity<Robust.Shared.Map.Components.MapGridComponent>(
                map.GridCoords.EntityId,
                entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
            var originTile = mapSys.LocalToTile(gridEnt.Owner, gridEnt.Comp,
                entMan.GetComponent<TransformComponent>(map.GridCoords.EntityId).Coordinates);
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    mapSys.SetTile(gridEnt, originTile + new Robust.Shared.Maths.Vector2i(dx, dy), floorTile);
                }
            }

            var deployEv = new MalfAiRobotFactoryEvent
            {
                Performer = body,
                Target = map.GridCoords.Offset(new System.Numerics.Vector2(1, 0)),
                Action = new Entity<ActionComponent>(action, actComp)
            };
            entMan.EventBus.RaiseLocalEvent(body, deployEv);

            var factories = FindFactories(pair, map);
            Assert.That(factories.Count, Is.EqualTo(1), "Factory was not deployed on the test grid.");
            var factory = factories[0];
            entMan.System<PowerReceiverSystem>().SetNeedsPower(factory, false);

            entMan.GetComponent<MalfAiFactoryComponent>(factory).ConversionTime = 1f;
            entMan.GetComponent<MalfAiFactoryComponent>(factory).InsertionDelayPerMass = 0f;

            var session = pair.Player;
            Assert.That(session, Is.Not.Null, "Connected pair has no player session.");
            var mindSys = entMan.System<Content.Server.Mind.MindSystem>();
            corpse = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.2f, 0f)));
            var corpseMind = mindSys.CreateMind(null);
            mindSys.TransferTo(corpseMind, corpse, mind: corpseMind);
            mindSys.SetUserId(corpseMind, session!.UserId);
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(corpse, Content.Shared.Mobs.MobState.Dead);
            _transferMindId = corpseMind;
            _factoryEnt = factory;

            feeder = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(1.1f, 0f)));
        });

        await server.WaitRunTicks(5);
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var factory = _factoryEnt;
            var dropEv = new Content.Shared.DragDrop.DragDropTargetEvent(feeder, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref dropEv);
            Assert.That(entMan.HasComponent<ActiveMalfAiFactoryComponent>(factory), Is.True,
                "Factory did not start grinding the corpse.");
        });

        await server.WaitRunTicks(2000);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            EntityUid? borg = null;
            var chassisQuery = entMan.AllEntityQueryEnumerator<Content.Shared.Silicons.Borgs.Components.BorgChassisComponent>();
            while (chassisQuery.MoveNext(out var chassis, out _))
            {
                if (entMan.GetComponent<TransformComponent>(chassis).GridUid == map.GridCoords.EntityId)
                    borg = chassis;
            }

            Assert.That(borg, Is.Not.Null, "The factory did not produce a cyborg.");
            var mindSys = entMan.System<SharedMindSystem>();
            Assert.That(mindSys.TryGetMind(borg!.Value, out var borgMindId, out _), Is.True,
                "The converted cyborg has no mind.");
            Assert.That(borgMindId, Is.EqualTo(_transferMindId),
                "The cyborg does not carry the corpse player's mind.");
            Assert.That(entMan.HasComponent<GhostRoleComponent>(borg!.Value), Is.False,
                "Ghost role was left open on the crewed borg (ownership leak on disconnect).");

            producedBorg = borg.Value;
            previewCorpse = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.System<Content.Shared.Mobs.Systems.MobStateSystem>()
                .ChangeMobState(previewCorpse, Content.Shared.Mobs.MobState.Dead);
            previewCrewBorg = entMan.SpawnEntity("BorgChassisGeneric", map.GridCoords);

            entMan.System<SharedRoleSystem>()
                .MindRemoveRole<SubvertedSiliconRoleComponent>(borgMindId);
        });

        await pair.RunTicksSync(10);
        await pair.RunUntilSynced();
        await pair.Client.WaitAssertion(() =>
        {
            var entMan = pair.Client.EntMan;
            var borg = pair.ToClientUid(producedBorg);
            var corpse = pair.ToClientUid(previewCorpse);
            var factory = pair.ToClientUid(_factoryEnt);
            var convertedPreview = new Content.Shared.DragDrop.CanDropTargetEvent(borg, corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref convertedPreview);
            Assert.That(convertedPreview.CanDrop, Is.True, "Client rejected the converted borg's factory preview.");
            var crewPreview = new Content.Shared.DragDrop.CanDropTargetEvent(pair.ToClientUid(previewCrewBorg), corpse);
            entMan.EventBus.RaiseLocalEvent(factory, ref crewPreview);
            Assert.That(crewPreview.CanDrop, Is.True, "Client rejected a crew borg's factory preview.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfPowerSiphonDrainsApc()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid siphonHost = default;
        string addedNodeId = null;
        Node foreignNode = null;
        Node cableNode = null;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var machine = entMan.SpawnEntity("Autolathe", map.GridCoords);
            var machineNodes = new NodeContainerComponent();
            var mapping = new MappingDataNode();
            mapping.Add("nodeGroupID", new ValueDataNode("HVPower"));
            var existingHvNode = server.ResolveDependency<ISerializationManager>()
                .Read<CableDeviceNode>(mapping, notNullableOverride: true);
            machineNodes.Nodes.Add("malfai_hv", existingHvNode);
            entMan.AddComponent(machine, machineNodes);

            var cable = entMan.SpawnEntity("CableHV", map.GridCoords);
            var apc = entMan.SpawnEntity("APCBasic", map.GridCoords);
            cableNode = entMan.GetComponent<NodeContainerComponent>(cable).Nodes["power"];
            foreignNode = machineNodes.Nodes["malfai_hv"];

            Buy(pair, store, body, MalfAiConstants.PowerSiphonListing);
            var action = GrantedAction(pair, body, "ActionMalfAiPowerSiphon");

            var actionsSys = entMan.System<Content.Shared.Actions.SharedActionsSystem>();
            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            var actionComp = entMan.GetComponent<ActionComponent>(action);
            actionsSys.PerformAction((body, actionsComp), (action, actionComp),
                new MalfAiPowerSiphonEvent { Performer = body, Target = machine }, predicted: false);

            var siphon = entMan.GetComponent<MalfAiPowerSiphonComponent>(machine);
            addedNodeId = siphon.AddedHvNodeId;
            Assert.That(addedNodeId, Is.Not.Null.And.Not.EqualTo("malfai_hv"));
            Assert.That(machineNodes.Nodes[addedNodeId!], Is.Not.SameAs(foreignNode));
            Assert.That(machineNodes.Nodes[addedNodeId!], Is.TypeOf<CableDeviceNode>());
            Assert.That(machineNodes.Nodes[addedNodeId!].NodeGroupID, Is.EqualTo(NodeGroupID.HVPower));
            siphonHost = machine;

            Assert.That(entMan.TryGetComponent<PowerConsumerComponent>(machine, out var consumer), Is.True);
            Assert.That(consumer!.NodeId, Is.EqualTo(addedNodeId));
            var startDraw = consumer.DrawRate;
            Assert.That(startDraw, Is.GreaterThan(0f));
            Assert.That(consumer.Voltage, Is.EqualTo(Content.Shared.Power.Voltage.High));

            Assert.That(entMan.TryGetComponent<TimedSpawnerComponent>(machine, out var sparks));
            Assert.That(sparks!.Prototypes, Does.Contain("EffectSparks"));
            Assert.That(sparks.IntervalSeconds, Is.EqualTo(TimeSpan.FromSeconds(3)));

            var cooldown = entMan.GetComponent<ActionComponent>(action).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Power Siphon did not start its cooldown.");
            Assert.That(cooldown.Value.End - cooldown.Value.Start, Is.EqualTo(TimeSpan.FromMinutes(5)));

            var ev = new MalfAiPowerSiphonEvent { Performer = body, Target = machine };
            entMan.EventBus.RaiseLocalEvent(body, ev);
            Assert.That(ev.Handled, Is.False, "Power Siphon fired twice on the same machine.");

            var locker = entMan.SpawnEntity("LockerSteel", map.GridCoords);
            var validateTarget = entMan.GetComponent<EntityTargetActionComponent>(action);
            Assert.That(actionsSys.ValidateEntityTarget(body, locker, (action, validateTarget)), Is.False,
                "Power Siphon passes engine validation on a locker.");
        });

        await server.WaitRunTicks(30);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(addedNodeId, Is.Not.Null);
            Assert.That(cableNode, Is.Not.Null);
            Assert.That(cableNode!.NodeGroup, Is.Not.Null);

            var nodes = entMan.GetComponent<NodeContainerComponent>(siphonHost);
            var addedNode = nodes.Nodes[addedNodeId!];
            Assert.That(addedNode.NodeGroup, Is.SameAs(cableNode.NodeGroup),
                "The siphon node did not join the same-tile HV cable network.");
            Assert.That(entMan.TryGetComponent<PowerConsumerComponent>(siphonHost, out var consumer), Is.True);
            Assert.That(consumer!.Net, Is.SameAs(cableNode.NodeGroup));
            Assert.That(consumer.DrawRate, Is.GreaterThan(50_000f),
                "Power siphon DrawRate did not ramp up.");

            entMan.RemoveComponent<MalfAiPowerSiphonComponent>(siphonHost);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var nodes = entMan.GetComponent<NodeContainerComponent>(siphonHost);
            Assert.That(entMan.HasComponent<PowerConsumerComponent>(siphonHost), Is.False);
            Assert.That(nodes.Nodes.ContainsKey(addedNodeId!), Is.False,
                "Siphon cleanup left its generated node behind.");
            Assert.That(nodes.Nodes["malfai_hv"], Is.SameAs(foreignNode),
                "Siphon cleanup removed the pre-existing colliding node.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfPowerSiphonStarvesWithoutPower()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid siphonHost = default;
        string addedNodeId = null;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var machine = entMan.SpawnEntity("Autolathe", map.GridCoords);
            entMan.SpawnEntity("CableHV", map.GridCoords);

            Buy(pair, store, body, MalfAiConstants.PowerSiphonListing);
            var action = GrantedAction(pair, body, "ActionMalfAiPowerSiphon");

            var actionsSys = entMan.System<Content.Shared.Actions.SharedActionsSystem>();
            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            var actionComp = entMan.GetComponent<ActionComponent>(action);
            actionsSys.PerformAction((body, actionsComp), (action, actionComp),
                new MalfAiPowerSiphonEvent { Performer = body, Target = machine }, predicted: false);

            var siphon = entMan.GetComponent<MalfAiPowerSiphonComponent>(machine);
            addedNodeId = siphon.AddedHvNodeId;
            Assert.That(addedNodeId, Is.Not.Null);
            siphon.StallMinDraw = 1000f;
            siphon.StallTimeoutSeconds = 0.5f;
            siphonHost = machine;
        });

        await server.WaitRunTicks(60);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.HasComponent<MalfAiPowerSiphonComponent>(siphonHost), Is.False,
                "Starved power siphon was not removed.");
            Assert.That(entMan.HasComponent<PowerConsumerComponent>(siphonHost), Is.False);
            Assert.That(entMan.HasComponent<NodeContainerComponent>(siphonHost), Is.False,
                "Starved siphon cleanup left its added NodeContainer behind.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfEmagRemotesEmag()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var alarm = entMan.SpawnEntity("FireAlarm", map.GridCoords);
            var pen = entMan.SpawnEntity("Pen", map.GridCoords);

            Buy(pair, store, body, MalfAiConstants.EmagListing);
            GrantedAction(pair, body, "ActionMalfAiEmag");

            bool Emag(EntityUid performer, EntityUid target)
            {
                var ev = new MalfAiEmagEvent { Performer = performer, Target = target };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled;
            }

            Assert.That(Emag(body, pen), Is.False,
                "Emag fired on an entity with no emag behavior.");
            Assert.That(entMan.HasComponent<EmaggedComponent>(pen), Is.False,
                "Emag marked an entity with no emag behavior.");

            Assert.That(Emag(body, alarm), Is.True,
                "Emag did not fire on a fire alarm.");
            Assert.That(entMan.HasComponent<EmaggedComponent>(alarm), Is.True,
                "Emagged fire alarm carries no EmaggedComponent.");

            Assert.That(Emag(body, alarm), Is.False,
                "Emag fired twice on the same target.");
        });
    }
}
