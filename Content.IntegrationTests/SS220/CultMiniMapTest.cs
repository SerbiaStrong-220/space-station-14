// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.SS220.CultYogg.CultMiniMap;
using Content.Server.SS220.CultYogg.CultMiniMap;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CultYogg.Buildings;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.SS220;

public sealed class CultMiniMapTest : GameTest
{
    public override PoolSettings PoolSettings => PsDisconnected;

    [TestPrototypes]
    private const string HealthPrototype = """
- type: entity
  id: CultMiniMapHealthDummy
  components:
  - type: Damageable
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
    triggersAlerts: false
    allowRevives: true

- type: entity
  id: CultMiniMapConfiguredViewer
  components:
  - type: CultMiniMap
    pingDuration: 8
    pingCooldown: 3
    trackingRules:
    - component: CultYogg
      label: cult-mini-map-cultist
      icon: /Textures/Interface/NavMap/beveled_diamond.png
      color: Violet
      scale: 0.75
    - component: MobState
      prototypes:
      - CultMiniMapHealthDummy
      icon:
        sprite: SS220/Interface/Actions/cult_yogg.rsi
        state: migo_teleport
      color: White
      scale: 1.5
      markerType: Airlock
""";

    [Test]
    public void StructureNeighborsUseSourceGridTiles()
    {
        var grid = new NetEntity(1);
        var otherGrid = new NetEntity(2);
        var origin = new CultMiniMapStructureLocation(grid, Vector2i.Zero);
        var walls = new HashSet<CultMiniMapStructureLocation>
        {
            origin,
            new(grid, Vector2i.Up),
            new(otherGrid, Vector2i.Right),
        };

        var neighbors = CultMiniMapNavMapControl.GetStructureNeighbors(walls, origin);

        Assert.That(neighbors.HasFlag(CultMiniMapStructureNeighbors.North), Is.True);
        Assert.That(neighbors.HasFlag(CultMiniMapStructureNeighbors.East), Is.False,
            "Walls on different grids must not join even when their tile indices are adjacent.");
    }

    [Test]
    public async Task PrivateSnapshotIsNotStoredInPublicUiState()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            var owner = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(owner);
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(GetState(owner).TrackedEntities, Has.Count.EqualTo(1));
            Assert.That(SEntMan.GetComponent<UserInterfaceComponent>(owner).States,
                Does.Not.ContainKey(CultMiniMapUIKey.Key),
                "Ordinary UI states are replicated to outsiders in PVS, even when they cannot open the UI.");
            ui.CloseUi(owner, CultMiniMapUIKey.Key);
        });
    }

    private static PoolSettings ConnectedSettings => new() { Connected = true };

    [Test]
    [PairConfig(nameof(ConnectedSettings))]
    public async Task SnapshotReplicatesOnlyToOwner()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        EntityUid owner = default;
        EntityUid outsider = default;
        EntityUid member = default;

        await Server.WaitAssertion(() =>
        {
            owner = SEntMan.SpawnEntity(null, map.GridCoords);
            outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(owner);
            Server.PlayerMan.SetAttachedEntity(ServerSession!, outsider);
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(GetState(owner).TrackedEntities, Has.Count.EqualTo(1));
        });

        await Pair.RunTicksSync(10);
        var clientOwner = Pair.ToClientUid(owner);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.EntityExists(clientOwner), Is.True);
            Assert.That(CEntMan.HasComponent<CultMiniMapComponent>(clientOwner), Is.False);
            Assert.That(CEntMan.GetComponent<UserInterfaceComponent>(clientOwner).States,
                Does.Not.ContainKey(CultMiniMapUIKey.Key));
        });

        await Server.WaitPost(() =>
        {
            member = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(member);
        });
        await Pair.RunTicksSync(120);
        await Server.WaitAssertion(() => Assert.That(GetState(owner).TrackedEntities, Has.Count.EqualTo(2)));
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.HasComponent<CultMiniMapComponent>(clientOwner), Is.False));

        await Client.ExecuteCommand("fullstatereset");
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.HasComponent<CultMiniMapComponent>(clientOwner), Is.False);
            Assert.That(CEntMan.GetComponent<UserInterfaceComponent>(clientOwner).States,
                Does.Not.ContainKey(CultMiniMapUIKey.Key));
        });

        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, owner));
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
        {
            var state = CEntMan.GetComponent<CultMiniMapComponent>(clientOwner).State;
            Assert.That(state, Is.Not.Null);
            Assert.That(state.TrackedEntities, Has.Count.EqualTo(2));
            var window = Client.ResolveDependency<IUserInterfaceManager>().WindowRoot.Children
                .OfType<CultMiniMapWindow>().Single(control => control.IsOpen);
            Assert.That(window.FindControl<CultMiniMapNavMapControl>("NavMap").TrackedEntities, Has.Count.EqualTo(2));
            Assert.That(window.FindControl<Label>("MemberCount").Text, Is.EqualTo(Loc.GetString("cult-mini-map-count", ("count", 2))));
        });

        await Server.WaitPost(() => SEntMan.RemoveComponent<CultYoggComponent>(member));
        await Pair.RunTicksSync(120);
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.GetComponent<CultMiniMapComponent>(clientOwner).State.TrackedEntities,
                Has.Count.EqualTo(1));
            var window = Client.ResolveDependency<IUserInterfaceManager>().WindowRoot.Children
                .OfType<CultMiniMapWindow>().Single(control => control.IsOpen);
            Assert.That(window.FindControl<CultMiniMapNavMapControl>("NavMap").TrackedEntities, Has.Count.EqualTo(1));
            Assert.That(window.FindControl<Label>("MemberCount").Text, Is.EqualTo(Loc.GetString("cult-mini-map-count", ("count", 1))));
        });

        await Server.WaitPost(() => ui.CloseUi(owner, CultMiniMapUIKey.Key));
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.GetComponent<CultMiniMapComponent>(clientOwner).State, Is.Null));
    }

    [Test]
    public async Task WallContoursIgnoreEntityRotation()
    {
        var map = await Pair.CreateTestMap();
        var sourceMap = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var transform = SEntMan.System<SharedTransformSystem>();

        await Server.WaitAssertion(() =>
        {
            // Put the source grid on the viewer's map, with a different grid rotation.
            transform.SetParent(sourceMap.Grid, map.MapUid);
            transform.SetLocalPosition(sourceMap.Grid, new Vector2(10, 0));
            transform.SetLocalRotation(map.Grid, Angle.FromDegrees(30));
            transform.SetLocalRotation(sourceMap.Grid, Angle.FromDegrees(90));
            var mapSystem = SEntMan.System<SharedMapSystem>();
            mapSystem.SetTile(sourceMap.Grid, new Vector2i(0, 1), sourceMap.Tile.Tile);
            mapSystem.SetTile(sourceMap.Grid, new Vector2i(1, 0), sourceMap.Tile.Tile);
            var viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);
            var wall = SEntMan.SpawnEntity("WallCultYogg", sourceMap.GridCoords);
            var door = SEntMan.SpawnEntity("CultYoggDoor", new EntityCoordinates(sourceMap.Grid, 0, 1));
            var airlock = SEntMan.SpawnEntity("CultYoggAirlock", new EntityCoordinates(sourceMap.Grid, 1, 0));
            transform.SetLocalRotation(wall, Angle.FromDegrees(180));
            transform.SetLocalRotation(door, Angle.FromDegrees(90));
            transform.SetLocalRotation(airlock, Angle.FromDegrees(90));
            foreach (var entity in new[] { wall, door, airlock })
            {
                Assert.That(SEntMan.GetComponent<TransformComponent>(entity).GridUid,
                    Is.EqualTo(sourceMap.Grid.Owner));
            }
            Assert.That(SEntMan.GetComponent<TransformComponent>(airlock).LocalRotation,
                Is.EqualTo(Angle.FromDegrees(90)));
            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);

            foreach (var entity in new[] { wall, door })
            {
                var marker = state.TrackedEntities.Single(entry => entry.Entity == SEntMan.GetNetEntity(entity));
                Assert.That(marker.Rotation, Is.EqualTo((float) Angle.FromDegrees(60).Theta).Within(0.001f),
                    "Wall contour directions must follow the source grid, regardless of the wall's local rotation.");
            }

            var airlockMarker = state.TrackedEntities.Single(entry => entry.Entity == SEntMan.GetNetEntity(airlock));
            Assert.That(airlockMarker.Rotation, Is.EqualTo((float) Angle.FromDegrees(150).Theta).Within(0.001f),
                "Airlocks must retain their entity orientation.");
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [Test]
    public async Task PingsAreValidatedSharedByChannelAndExpire()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var tracking = SEntMan.System<CultMiniMapTrackingSystem>();
        EntityUid first = default;
        EntityUid second = default;
        EntityUid otherChannel = default;
        EntityUid remote = default;
        EntityUid outsider = default;
        uint firstPingId = default;

        await Server.WaitAssertion(() =>
        {
            first = SEntMan.SpawnEntity(null, map.GridCoords);
            second = SEntMan.SpawnEntity(null, map.GridCoords);
            otherChannel = SEntMan.SpawnEntity(null, map.GridCoords);
            remote = SEntMan.SpawnEntity(null, otherMap.GridCoords);
            outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            foreach (var owner in new[] { first, second, otherChannel, remote })
                SEntMan.AddComponent<CultYoggComponent>(owner);

            var firstMap = SEntMan.GetComponent<CultMiniMapComponent>(first);
            firstMap.PingCooldown = TimeSpan.FromSeconds(0.1);
            firstMap.PingDuration = TimeSpan.FromSeconds(0.5);
            firstMap.MaxActivePings = 1;
            var secondMap = SEntMan.GetComponent<CultMiniMapComponent>(second);
            secondMap.PingCooldown = TimeSpan.FromSeconds(0.1);
            secondMap.PingDuration = TimeSpan.FromSeconds(0.5);
            secondMap.MaxActivePings = 3;
            SEntMan.GetComponent<CultMiniMapComponent>(otherChannel).PingChannel = "another-cult";

            foreach (var owner in new[] { first, second, otherChannel, remote })
                ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);

            var coordinates = new EntityCoordinates(map.Grid, Vector2.Zero);
            Assert.That(tracking.TryCreatePing((outsider, null), outsider, coordinates), Is.False,
                "A caller without the map component must be rejected.");
            Assert.That(tracking.TryCreatePing((first, firstMap), outsider, coordinates), Is.False,
                "Another actor must not publish through somebody else's map.");
            Assert.That(tracking.TryCreatePing((first, firstMap), first,
                new EntityCoordinates(otherMap.Grid, Vector2.Zero)), Is.False,
                "The client may only ping the grid currently displayed by its map.");
            Assert.That(tracking.TryCreatePing((first, firstMap), first,
                new EntityCoordinates(map.Grid, new Vector2(10000f, 10000f))), Is.False,
                "Coordinates outside the grid bounds must be rejected.");
            var bounds = SEntMan.GetComponent<MapGridComponent>(map.Grid).LocalAABB;
            Assert.That(tracking.TryCreatePing((first, firstMap), first,
                new EntityCoordinates(map.Grid, new Vector2(bounds.Right + 0.5f, bounds.Center.Y))), Is.False,
                "Coordinates in the old enlarged-AABB band must be rejected.");
            Assert.That(tracking.TryCreatePing((first, firstMap), first, coordinates), Is.True);
            Assert.That(tracking.TryCreatePing((first, firstMap), first, coordinates), Is.False,
                "The server must enforce the cooldown.");

            var firstState = GetState(first);
            Assert.That(firstState.Pings, Has.Count.EqualTo(1));
            var ping = firstState.Pings.Single();
            firstPingId = ping.Id;
            Assert.That(ping.Coordinates.NetEntity,
                Is.EqualTo(SEntMan.GetNetEntity(map.Grid)));
            Assert.That(ping.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_circle.png"))));
            Assert.That(ping.Color, Is.EqualTo(Color.DeepSkyBlue));
            Assert.That(GetState(second).Pings.Select(ping => ping.Id), Does.Contain(firstPingId));
            Assert.That(GetState(otherChannel).Pings, Is.Empty);
            Assert.That(GetState(remote).Pings, Is.Empty,
                "A shared channel must not reveal positions on a different map.");
        });

        await Server.WaitRunTicks(15);
        await Server.WaitAssertion(() =>
        {
            var coordinates = new EntityCoordinates(map.Grid, Vector2.Zero);
            var firstMap = SEntMan.GetComponent<CultMiniMapComponent>(first);
            var secondMap = SEntMan.GetComponent<CultMiniMapComponent>(second);
            Assert.That(tracking.TryCreatePing((first, firstMap), first, coordinates), Is.True);
            Assert.That(tracking.TryCreatePing((second, secondMap), second, coordinates), Is.True);
            var firstPings = GetState(first).Pings;
            Assert.That(firstPings, Has.Count.EqualTo(1));
            Assert.That(firstPings.Select(ping => ping.Id), Does.Not.Contain(firstPingId));
            var secondPings = GetState(second).Pings;
            Assert.That(secondPings, Has.Count.EqualTo(3));
            Assert.That(secondPings.Select(ping => ping.Id), Does.Contain(firstPingId),
                "One owner's display limit must not evict shared channel history for another owner.");
        });

        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            Assert.That(GetState(first).Pings, Is.Empty);
            Assert.That(GetState(second).Pings, Is.Empty);
            foreach (var owner in new[] { first, second, otherChannel, remote })
                ui.CloseUi(owner, CultMiniMapUIKey.Key);
            // Pair only tracks the last map for automatic cleanup.
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [Test]
    public async Task ConfiguredRulesSelectTargetsAndAppearances()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            var viewer = SEntMan.SpawnEntity("CultMiniMapConfiguredViewer", map.GridCoords);
            var configuration = SEntMan.GetComponent<CultMiniMapComponent>(viewer);
            Assert.That(configuration.PingDuration, Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(configuration.PingCooldown, Is.EqualTo(TimeSpan.FromSeconds(3)));
            var cultist = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(cultist);
            var mob = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            var both = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(both);
            var filteredOut = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<MobStateComponent>(filteredOut);
            var miGo = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<MiGoComponent>(miGo);

            Assert.That(SEntMan.GetComponent<CultMiniMapComponent>(viewer).MiniMapActionEntity, Is.Not.Null);
            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(mob), Is.False,
                "Being tracked does not grant the ability to view the map.");
            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            var duplicateOpen = new OpenBoundInterfaceMessage
            {
                Actor = viewer,
                UiKey = CultMiniMapUIKey.Key,
                Entity = SEntMan.GetNetEntity(viewer),
            };
            SEntMan.EventBus.RaiseLocalEvent(viewer, duplicateOpen);
            Assert.That(GetState(viewer), Is.SameAs(state),
                "A duplicate open message must not rebuild an already initialized minimap state.");
            Assert.That(state.TrackedEntities.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist),
                SEntMan.GetNetEntity(mob), SEntMan.GetNetEntity(both),
            }), "Custom rules replace the defaults, while the viewer remains in their own section.");

            var selfMarker = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(viewer)).Marker;
            Assert.That(selfMarker.Component, Is.EqualTo(CultMiniMapMarker.SelfComponent));
            Assert.That(selfMarker.RuleIndex, Is.EqualTo(CultMiniMapMarker.SelfRuleIndex));
            Assert.That(selfMarker.Label?.ToString(), Is.EqualTo("cult-mini-map-self-section"));
            Assert.That(selfMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_star.png"))));
            Assert.That(selfMarker.Color, Is.EqualTo(Color.Cyan));
            Assert.That(selfMarker.Scale, Is.EqualTo(1.2f));
            Assert.That(selfMarker.MarkerType, Is.EqualTo(CultMiniMapMarkerType.Icon));

            var cultMarker = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Marker;
            Assert.That(cultMarker.Component, Is.EqualTo("CultYogg"));
            Assert.That(cultMarker.RuleIndex, Is.EqualTo(0));
            Assert.That(cultMarker.Label?.ToString(), Is.EqualTo("cult-mini-map-cultist"));
            Assert.That(cultMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_diamond.png"))));
            Assert.That(cultMarker.Color, Is.EqualTo(Color.Violet));
            Assert.That(cultMarker.Scale, Is.EqualTo(0.75f));
            Assert.That(cultMarker.MarkerType, Is.EqualTo(CultMiniMapMarkerType.Icon));
            Assert.That(state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(both)).Marker.Component,
                Is.EqualTo("CultYogg"), "The first matching rule wins; there must be no duplicate markers.");

            var mobMarker = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(mob)).Marker;
            Assert.That(mobMarker.Component, Is.EqualTo("MobState"));
            Assert.That(mobMarker.RuleIndex, Is.EqualTo(1));
            Assert.That(mobMarker.Label, Is.Null);
            Assert.That(mobMarker.Icon, Is.EqualTo(new SpriteSpecifier.Rsi(
                new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "migo_teleport")));
            Assert.That(mobMarker.Color, Is.EqualTo(Color.White));
            Assert.That(mobMarker.Scale, Is.EqualTo(1.5f));
            Assert.That(mobMarker.MarkerType, Is.EqualTo(CultMiniMapMarkerType.Airlock));

            // Configuration is per observer; another map still uses its own defaults.
            ui.OpenUi(cultist, CultMiniMapUIKey.Key, cultist);
            Assert.That(GetState(cultist).TrackedEntities.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(cultist), SEntMan.GetNetEntity(both), SEntMan.GetNetEntity(miGo),
            }));
            ui.CloseUi(cultist, CultMiniMapUIKey.Key);
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
        });
    }

    [Test]
    public async Task DefaultBuildingRulesUsePrototypeSpecificMarkers()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            var viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);

            var expected = new[]
            {
                (Prototype: "WallCultYogg", Label: "cult-mini-map-wall", Type: CultMiniMapMarkerType.Wall, Icon: default(string)),
                (Prototype: "CultYoggDoor", Label: "cult-mini-map-secret-door", Type: CultMiniMapMarkerType.SecretDoor, Icon: default(string)),
                (Prototype: "CultYoggAirlock", Label: "cult-mini-map-airlock", Type: CultMiniMapMarkerType.Airlock, Icon: default(string)),
                (Prototype: "CultYoggPod", Label: "cult-mini-map-pod", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_pod.png"),
                (Prototype: "CultYoggFungusHydroponic", Label: "cult-mini-map-fungus", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_fungus.png"),
                (Prototype: "CultYoggAltar", Label: "cult-mini-map-altar", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_altar.png"),
                (Prototype: "CultYoggPond", Label: "cult-mini-map-pond", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_pond.png"),
                (Prototype: "VoidTeleportEnter", Label: "cult-mini-map-teleporter", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_gate.png"),
                (Prototype: "VoidTeleportExit", Label: "cult-mini-map-teleporter", Type: CultMiniMapMarkerType.Icon, Icon: "/Textures/SS220/Interface/NavMap/cult_gate.png"),
            };
            var buildings = expected.Select(entry =>
                (Entity: SEntMan.SpawnEntity(entry.Prototype, map.GridCoords), Entry: entry)).ToList();

            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            Assert.That(state.TrackedEntities, Has.Count.EqualTo(buildings.Count + 1));

            foreach (var (entity, entry) in buildings)
            {
                var trackedEntity = state.TrackedEntities
                    .Single(candidate => candidate.Entity == SEntMan.GetNetEntity(entity));
                var marker = trackedEntity.Marker;
                Assert.That(marker.Label?.ToString(), Is.EqualTo(entry.Label), entry.Prototype);
                Assert.That(marker.MarkerType, Is.EqualTo(entry.Type), entry.Prototype);
                Assert.That(marker.ShowInList, Is.False, entry.Prototype);
                Assert.That(marker.ShowHealth, Is.False, entry.Prototype);
                Assert.That(marker.Color, Is.EqualTo(Color.Red), entry.Prototype);
                if (entry.Type == CultMiniMapMarkerType.Icon)
                {
                    Assert.That(marker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResPath(entry.Icon!))), entry.Prototype);
                    Assert.That(marker.Scale, Is.EqualTo(1f), entry.Prototype);
                    Assert.That(trackedEntity.StructureLocation, Is.Null, entry.Prototype);
                }
                else
                {
                    Assert.That(trackedEntity.StructureLocation?.Grid,
                        Is.EqualTo(SEntMan.GetNetEntity(map.Grid)), entry.Prototype);
                }
            }

            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
        });
    }

    [Test]
    public async Task ConfigurationChangesUpdateOpenMap()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        EntityUid viewer = default;
        EntityUid target = default;
        CultMiniMapMarker oldMarker = default!;

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity("CultMiniMapConfiguredViewer", map.GridCoords);
            target = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(target);
            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            oldMarker = GetState(viewer).TrackedEntities
                .Single(member => member.Entity == SEntMan.GetNetEntity(target)).Marker;

            var rules = SEntMan.GetComponent<CultMiniMapComponent>(viewer).TrackingRules!;
            rules[0].Color = Color.Red;
            rules.RemoveAt(0);
            rules[0].Color = Color.Green;
            rules[0].Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_star.png"));
            rules[0].Scale = 0f;
            // Invalid names can be entered through runtime edits; they must not break other rules.
            rules.Insert(0, new CultMiniMapTrackingRule { ComponentName = "NonexistentMiniMapTestComponent" });
        });

        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var marker = GetState(viewer).TrackedEntities
                .Single(member => member.Entity == SEntMan.GetNetEntity(target)).Marker;
            Assert.That(marker.Component, Is.EqualTo("MobState"));
            Assert.That(marker.Color, Is.EqualTo(Color.Green));
            Assert.That(marker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_star.png"))));
            Assert.That(marker.Scale, Is.EqualTo(1f), "Invalid sizes must not hide or corrupt map markers.");
            Assert.That(oldMarker.Component, Is.EqualTo("CultYogg"));
            Assert.That(oldMarker.Color, Is.EqualTo(Color.Violet), "Published states must not change with the config.");
            SEntMan.GetComponent<CultMiniMapComponent>(viewer).TrackingRules!.Clear();
        });

        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var members = GetState(viewer).TrackedEntities;
            Assert.That(members, Has.Count.EqualTo(1));
            Assert.That(members.Single().Entity, Is.EqualTo(SEntMan.GetNetEntity(viewer)));
            Assert.That(members.Single().Marker.Component, Is.EqualTo(CultMiniMapMarker.SelfComponent));
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
        });
    }

    [Test]
    public async Task HealthUpdatesWithoutSensorsAndHandlesMissingData()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var damage = SEntMan.System<DamageableSystem>();
        var thresholds = SEntMan.System<MobThresholdSystem>();
        EntityUid viewer = default;
        EntityUid target = default;
        EntityUid miGo = default;
        EntityUid noThreshold = default;
        EntityUid building = default;

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);
            // Health remains available even when coordinates cannot be shown on this map.
            target = SEntMan.SpawnEntity("CultMiniMapHealthDummy", otherMap.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(target);
            miGo = SEntMan.SpawnEntity("MobMiGo", map.GridCoords);
            damage.SetDamage(miGo, new DamageSpecifier { DamageDict = { ["Blunt"] = 70 } });
            noThreshold = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<MiGoComponent>(noThreshold);
            SEntMan.AddComponent<MobStateComponent>(noThreshold);
            SEntMan.AddComponent<DamageableComponent>(noThreshold);
            building = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            SEntMan.AddComponent<CultYoggBuildingComponent>(building);

            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            var self = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(viewer));
            Assert.That(self.HealthState, Is.EqualTo(MobState.Invalid));
            Assert.That(self.DamagePercentage, Is.Null);
            var noThresholdState = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(noThreshold));
            Assert.That(noThresholdState.HealthState, Is.EqualTo(MobState.Alive));
            Assert.That(noThresholdState.DamagePercentage, Is.Null);
            var miGoState = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(miGo));
            Assert.That(miGoState.HealthState, Is.EqualTo(MobState.Alive));
            Assert.That(miGoState.DamagePercentage, Is.EqualTo(0.5f).Within(0.001f));
            var buildingState = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(building));
            Assert.That(buildingState.Marker.Component, Is.EqualTo("CultYoggBuilding"));
            Assert.That(buildingState.Marker.ShowInList, Is.False);
            Assert.That(buildingState.Marker.ShowHealth, Is.False);
            Assert.That(buildingState.Marker.MarkerType, Is.EqualTo(CultMiniMapMarkerType.Icon));
            Assert.That(buildingState.HealthState, Is.EqualTo(MobState.Invalid),
                "Map-only rules must not publish health even when the entity has mob health components.");
            Assert.That(buildingState.DamagePercentage, Is.Null);
        });

        // Exercise damage, critical state, death and healing through the normal damage system.
        foreach (var (amount, expectedState) in new[]
        {
            (0, MobState.Alive), (50, MobState.Alive), (110, MobState.Critical),
            (210, MobState.Dead), (0, MobState.Alive),
        })
        {
            await Server.WaitPost(() =>
                damage.SetDamage(target, new DamageSpecifier { DamageDict = { ["Blunt"] = amount } }));
            await Server.WaitRunTicks(120);
            await Server.WaitAssertion(() =>
            {
                var member = GetState(viewer).TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(target));
                Assert.That(member.HealthState, Is.EqualTo(expectedState));
                Assert.That(member.DamagePercentage, Is.EqualTo(amount / 100f).Within(0.001f));
                Assert.That(member.Coordinates, Is.Null);
            });
        }

        await Server.WaitPost(() => thresholds.SetMobStateThreshold(target, 0, MobState.Critical));
        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var member = GetState(viewer).TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(target));
            Assert.That(member.DamagePercentage, Is.Null, "A zero threshold must not produce NaN or infinity.");
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [Test]
    public async Task MembersAndPositionsUpdateWithoutSuitSensors()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        EntityUid viewer = default;
        EntityUid cultist = default;
        EntityUid miGo = default;
        EntityUid both = default;
        EntityUid outsider = default;
        EntityUid remote = default;
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var transform = SEntMan.System<SharedTransformSystem>();

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            cultist = SEntMan.SpawnEntity(null, new EntityCoordinates(map.Grid, 200, 0));
            miGo = SEntMan.SpawnEntity(null, map.GridCoords);
            both = SEntMan.SpawnEntity(null, map.GridCoords);
            outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            remote = SEntMan.SpawnEntity(null, otherMap.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);
            SEntMan.AddComponent<CultYoggComponent>(cultist);
            SEntMan.AddComponent<MiGoComponent>(miGo);
            SEntMan.AddComponent<CultYoggComponent>(both);
            SEntMan.AddComponent<MiGoComponent>(both);
            SEntMan.AddComponent<MiGoComponent>(remote);

            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            Assert.That(state.TrackedEntities.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist), SEntMan.GetNetEntity(miGo),
                SEntMan.GetNetEntity(both), SEntMan.GetNetEntity(remote),
            }));
            var miGoMarker = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(both)).Marker;
            Assert.That(miGoMarker.Component, Is.EqualTo("MiGo"));
            Assert.That(miGoMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/SS220/Interface/NavMap/migo.png"))));
            Assert.That(state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(remote)).Coordinates, Is.Null);
            var position = state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates;
            Assert.That(position.HasValue, Is.True);
            Assert.That(position.Value.NetEntity, Is.EqualTo(SEntMan.GetNetEntity(map.Grid)));
            Assert.That(position.Value.Position.X, Is.EqualTo(200f));

            transform.SetCoordinates(cultist, new EntityCoordinates(map.Grid, 250, 0));
            SEntMan.RemoveComponent<CultYoggComponent>(both);
            SEntMan.RemoveComponent<MiGoComponent>(both);
            SEntMan.DeleteEntity(miGo);
            SEntMan.AddComponent<CultYoggComponent>(outsider);
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.TrackedEntities.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist),
                SEntMan.GetNetEntity(outsider), SEntMan.GetNetEntity(remote),
            }));
            Assert.That(state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates.Value.Position.X,
                Is.EqualTo(250f));

            transform.SetCoordinates(viewer, otherMap.GridCoords);
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.Grid, Is.EqualTo(SEntMan.GetNetEntity(otherMap.Grid)));
            Assert.That(state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(remote)).Coordinates, Is.Not.Null);
            Assert.That(state.TrackedEntities.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates, Is.Null);
            transform.SetCoordinates(viewer, new EntityCoordinates(otherMap.MapUid, 50, 0));
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.Grid, Is.Null);
            Assert.That(state.TrackedEntities, Has.Count.EqualTo(4));
            Assert.That(state.TrackedEntities.All(member => member.Coordinates == null), Is.True);
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
            Assert.That(SEntMan.GetComponent<CultMiniMapComponent>(viewer).State, Is.Null);
            // Pair only tracks the last map for automatic cleanup.
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AbilityFollowsEitherMembershipAndIsPrivate(bool cultistFirst)
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            // Cover both conversion of an existing entity and the normal Mi-Go spawn path.
            var owner = SEntMan.SpawnEntity(cultistFirst ? null : "MobMiGo", map.GridCoords);
            var outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            if (cultistFirst)
                SEntMan.AddComponent<CultYoggComponent>(owner);
            var ability = SEntMan.GetComponent<CultMiniMapComponent>(owner);
            var action = ability.MiniMapActionEntity;
            Assert.That(action, Is.Not.Null);
            Assert.That(SEntMan.GetComponent<ActionsComponent>(owner).Actions, Does.Contain(action.Value));
            ui.OpenUi(owner, CultMiniMapUIKey.Key, outsider);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);

            if (cultistFirst)
                SEntMan.AddComponent<MiGoComponent>(owner);
            else
                SEntMan.AddComponent<CultYoggComponent>(owner);

            Assert.That(SEntMan.GetComponent<CultMiniMapComponent>(owner).MiniMapActionEntity, Is.EqualTo(action));
            if (cultistFirst)
                SEntMan.RemoveComponent<CultYoggComponent>(owner);
            else
                SEntMan.RemoveComponent<MiGoComponent>(owner);

            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(owner), Is.True);
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.True);

            if (cultistFirst)
                SEntMan.RemoveComponent<MiGoComponent>(owner);
            else
                SEntMan.RemoveComponent<CultYoggComponent>(owner);

            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(owner), Is.False);
            Assert.That(SEntMan.GetComponent<ActionsComponent>(owner).Actions, Does.Not.Contain(action.Value));
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);
            Assert.That(SEntMan.GetComponent<UserInterfaceComponent>(owner).States, Does.Not.ContainKey(CultMiniMapUIKey.Key));
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);
        });
    }

    private CultMiniMapState GetState(EntityUid owner)
    {
        var state = SEntMan.GetComponent<CultMiniMapComponent>(owner).State;
        Assert.That(state, Is.Not.Null);
        return state;
    }
}
