// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.IntegrationTests.Pair;
using Content.Server.Database;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Content.IntegrationTests.SS220.Tests.Zones;

[TestFixture]
[TestOf(typeof(SharedZonesSystem))]
public sealed class ZoneTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: ZoneTest
  id: ZoneTest
  components:
  - type: Zone

- type: entity
  name: ZoneItemTest
  id: ZoneItemTest
  components:
  - type: Item
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.25,-0.25,0.25,0.25""
        density: 20
        mask:
        - ItemMask
        restitution: 0.3  # fite me
        friction: 0.2
";

    [Test]
    public async Task CreateChangeInitializeDeleteZoneTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap(false);

        var entMng = server.ResolveDependency<IEntityManager>();
        var mapMng = server.ResolveDependency<IMapManager>();
        var mapSys = entMng.System<SharedMapSystem>();
        var zoneSys = entMng.System<ZonesSystem>();

        Entity<MapGridComponent> grid = default;
        await server.WaitPost(() =>
        {
            grid = mapMng.CreateGridEntity(map.MapId);
            var tiles = GetTiles(new Vector2i(5, 5));
            mapSys.SetTiles(grid, tiles);
        });

        // Try create
        ZoneParams zoneParams = default;
        Entity<ZoneComponent> zone = default;
        Entity<ZonesContainerComponent> container = default;
        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            var zoneBox = new Box2(1, 1, 4, 4);
            zoneParams = new ZoneParams()
            {
                Container = grid.Owner,
                ProtoID = "ZoneTest",
                Name = "TestZone"
            };
            zoneParams.SetOriginalSize([zoneBox]);

            var (newZone, failReason) = zoneSys.CreateZone(zoneParams);
            Assert.That(newZone, Is.Not.Null, $"Failed to create a new zone by reason: \"{failReason ?? "Unknown"}\"");
            zone = newZone.Value;

            Assert.That(entMng.HasComponent<ZoneComponent>(newZone), Is.True, $"Zone entity {zone.Owner} doesn't has a {nameof(ZoneComponent)}");
            Assert.That(zoneParams.Equals(zone.Comp.ZoneParams), Is.True, $"Zone params changed after creating zone");
            Assert.That(entMng.HasComponent<ZonesContainerComponent>(grid), Is.True, $"After creating a zone {entMng.ToPrettyString(grid)} didn't get a {nameof(ZonesContainerComponent)}");

            container = (grid, entMng.GetComponent<ZonesContainerComponent>(grid));
        }));

        // Try change
        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            var zoneBox = new Box2(3, 3, 4, 4);
            zoneParams.SetOriginalSize([zoneBox]);
            zoneParams.AttachToGrid = true;
            zoneParams.CutSpaceOption = ZoneParams.CutSpaceOptions.Dinamic;

            var shouldRecreate = SharedZonesSystem.NeedRecreate(zone.Comp.ZoneParams, zoneParams);
            zoneSys.ChangeZone(zone, zoneParams);

            if (shouldRecreate)
            {
                var zoneUid = entMng.GetEntity(container.Comp.Zones.First());
                Assert.That(container.Comp.Zones, Has.Count.EqualTo(1));
                Assert.That(zoneUid, Is.Not.EqualTo(zone.Owner), "Zone didn't recreated");
                Assert.That(entMng.HasComponent<ZoneComponent>(zoneUid), Is.True, $"Recreated zone didn't has a {nameof(ZoneComponent)}");
                var zoneComp = entMng.GetComponent<ZoneComponent>(zoneUid);
                zone = (zoneUid, zoneComp);
            }

            Assert.That(zoneParams.Equals(zone.Comp.ZoneParams), Is.True, $"Zone params doesn't changed after calling {nameof(zoneSys.ChangeZone)}");
        }));

        // Try initialize and test in zone entity
        EntityUid item = default;
        await server.WaitAssertion(() =>
        {
            var coords = new EntityCoordinates(grid, 3.5f, 3.5f);
            item = entMng.SpawnEntity("ZoneItemTest", coords);
            Assert.That(zoneSys.InZone(zone, item), Is.True);
        });
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(zone.Comp.EnteredEntities, Has.Count.EqualTo(0), $"Zone {entMng.ToPrettyString(zone)} updates entities on uninitialized map");
            Assert.That(zoneParams.Equals(zone.Comp.ZoneParams), $"Zone params changed after {nameof(server.WaitRunTicks)} on uninitialized map");
        }));

        await server.WaitPost(() => mapSys.InitializeMap(map.MapId));
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(zone.Comp.EnteredEntities, Is.Not.Empty, $"Zone {entMng.ToPrettyString(zone)} didn't update entities on initialized map");
            Assert.That(zone.Comp.EnteredEntities, Has.Count.EqualTo(1));
            Assert.That(zoneParams.Equals(zone.Comp.ZoneParams), $"Zone params changed after {nameof(server.WaitRunTicks)} on initialized map");
        }));

        // Try delete
        await server.WaitPost(() => zoneSys.DeleteZone(zone));
        await server.WaitRunTicks(1);
        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(entMng.Deleted(zone), Is.True, $"Zone didn't delete after calling {nameof(zoneSys.DeleteZone)}");
            Assert.That(zoneSys.GetZonesFromContainer((grid.Owner, container.Comp)), Has.Count.EqualTo(0));

            var itemXForm = entMng.GetComponent<TransformComponent>(item);
            Assert.That(zoneSys.GetZonesByPoint(itemXForm.Coordinates).ToList(), Has.Count.EqualTo(0));
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ZoneSaveLoadTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var path = new ResPath("/zone save load.yml");
        var map = await pair.CreateTestMap(false);
        var grid = map.Grid;

        var entMng = server.ResolveDependency<IEntityManager>();
        var mapMng = server.ResolveDependency<IMapManager>();
        var mapLoader = entMng.System<MapLoaderSystem>();
        var mapSys = entMng.System<SharedMapSystem>();
        var zoneSys = entMng.System<ZonesSystem>();

        ZoneParams zoneParams = default;
        await server.WaitPost(() =>
        {
            var tiles = GetTiles(new Vector2i(10, 10));
            mapSys.SetTiles(grid, tiles);

            var zoneBox = new Box2(3, 3, 7, 7);
            zoneParams = new ZoneParams()
            {
                Container = grid.Owner,
                ProtoID = "ZoneTest",
                Name = "TestZone"
            };
            zoneParams.SetOriginalSize([zoneBox]);
        });

        // Try save
        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            var (zone, failReason) = zoneSys.CreateZone(zoneParams);
            Assert.That(zone, Is.Not.Null, $"Failed to create a new zone by reason: \"{failReason ?? "Unknown"}\"");
            Assert.That(mapLoader.TrySaveGrid(grid, path));
            entMng.DeleteEntity(grid);
        }));

        // Try load
        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(mapLoader.TryLoadGrid(map.MapId, path, out var grid2));
            zoneParams.Container = grid2.Value.Owner;

            Assert.That(entMng.TryGetComponent<ZonesContainerComponent>(grid2, out var containerComp));
            Assert.That(containerComp.Zones, Has.Count.EqualTo(1));
            Assert.That(entMng.TryGetComponent<ZoneComponent>(entMng.GetEntity(containerComp.Zones.First()), out var zoneComp));
            Assert.That(zoneParams.Equals(zoneComp.ZoneParams));
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ZoneBoundsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var map = await pair.CreateTestMap();

        var entMng = server.ResolveDependency<IEntityManager>();
        var zoneSys = entMng.System<ZonesSystem>();
        var xForm = entMng.System<TransformSystem>();

        var container = map.MapUid;
        ZoneParams zoneParams = default;
        Entity<ZoneComponent> zone = default;
        await server.WaitPost(() =>
        {
            var zoneBox = new Box2(0, 0, 2, 2);
            zoneParams = new ZoneParams()
            {
                Container = container,
                ProtoID = "ZoneTest",
                Name = "TestZone"
            };
            zoneParams.SetOriginalSize([zoneBox]);

            var (newZone, failReason) = zoneSys.CreateZone(zoneParams);
            Assert.That(newZone, Is.Not.Null, $"Failed to create a new zone by reason: \"{failReason ?? "Unknown"}\"");
            zone = newZone.Value;
        });

        EntityUid item = default;
        await server.WaitAssertion(() =>
        {
            var coords = new EntityCoordinates(container, 0, 0);
            item = entMng.SpawnEntity("ZoneItemTest", coords);
            Assert.That(zoneSys.InZone(zone, item), Is.True);
        });
        var box = zone.Comp.ZoneParams.OriginalRegion.First();

        // Check inside box positions
        var points = GetNearestPoints(box, GetPointsOption.InsideBox);
        foreach (var point in points)
        {
            await server.WaitPost(() => xForm.SetLocalPosition(item, point));
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => Assert.Multiple(() =>
            {
                Assert.That(zoneSys.InZone(zone, item, ZoneParams.RegionType.Original));
                Assert.That(zoneSys.GetInZoneEntities(zone, ZoneParams.RegionType.Original).ToList(), Has.Count.EqualTo(1));
                Assert.That(zone.Comp.EnteredEntities, Has.Count.EqualTo(1));
            }));
        }

        // Check on border box positions
        points = GetNearestPoints(box, GetPointsOption.OnBorder);
        foreach (var point in points)
        {
            await server.WaitPost(() => xForm.SetLocalPosition(item, point));
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => Assert.Multiple(() =>
            {
                Assert.That(zoneSys.InZone(zone, item, ZoneParams.RegionType.Original));
                Assert.That(zoneSys.GetInZoneEntities(zone, ZoneParams.RegionType.Original).ToList(), Has.Count.EqualTo(1));
                Assert.That(zone.Comp.EnteredEntities, Has.Count.EqualTo(1));
            }));
        }

        // Check outside box positions
        points = GetNearestPoints(box, GetPointsOption.OutsideBox);
        foreach (var point in points)
        {
            await server.WaitPost(() => xForm.SetLocalPosition(item, point));
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() => Assert.Multiple(() =>
            {
                Assert.That(!zoneSys.InZone(zone, item, ZoneParams.RegionType.Original));
                Assert.That(zoneSys.GetInZoneEntities(zone, ZoneParams.RegionType.Original).ToList(), Has.Count.EqualTo(0));
                Assert.That(zone.Comp.EnteredEntities, Has.Count.EqualTo(0));
            }));
        }

        await pair.CleanReturnAsync();
    }

    private static List<(Vector2i Index, Tile Tile)> GetTiles(Vector2i size)
    {
        var list = new List<(Vector2i Index, Tile Tile)>();
        var tile = new Tile(1);
        var top = size.Y;
        var right = size.X;

        for (var y = 0; y < top; y++)
            for (var x = 0; x < right; x++)
                list.Add((new Vector2i(x, y), tile));

        return list;
    }

    private static List<Vector2> GetNearestPoints(Box2 box, GetPointsOption option, float offset = 0.01f, float step = 0.01f)
    {
        var result = new List<Vector2>();
        var vectorOffset = new Vector2(offset, offset);
        switch (option)
        {
            case GetPointsOption.InsideBox:
                box = Box2.FromTwoPoints(box.BottomLeft + vectorOffset, box.TopRight - vectorOffset);
                break;

            case GetPointsOption.OutsideBox:
                box = Box2.FromTwoPoints(box.BottomLeft - vectorOffset, box.TopRight + vectorOffset);
                break;

            default:
                break;
        }

        var xValues = GetValues(box.Left, box.Right, step);
        var yValues = GetValues(box.Bottom, box.Top, step);

        var bottomPoints = xValues.Select(n => new Vector2(n, box.Bottom));
        result.AddRange(bottomPoints);

        var leftPoints = yValues.Select(n => new Vector2(box.Left, n));
        result.AddRange(leftPoints);

        var topPoints = xValues.Select(n => new Vector2(n, box.Top));
        result.AddRange(topPoints);

        var rigntPoints = yValues.Select(n => new Vector2(box.Right, n));
        result.AddRange(rigntPoints);

        return result;

        List<float> GetValues(float start, float end, float step)
        {
            var result = new List<float>();
            var accumulate = start;
            while (accumulate < end)
            {
                result.Add(accumulate);
                accumulate += step;
            }

            result.Add(end);
            return result;
        }
    }

    private enum GetPointsOption
    {
        OnBorder,
        InsideBox,
        OutsideBox
    }
}
