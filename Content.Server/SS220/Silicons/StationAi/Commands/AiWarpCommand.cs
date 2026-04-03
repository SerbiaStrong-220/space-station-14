// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Shared.Administration;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Content.Shared.Warps;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

using Robust.Shared.Maths;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi;
using Content.Server.Radio.Components;

namespace Content.Server.SS220.Silicons.StationAi.Commands
{
    [AnyCommand]
    public sealed class AiWarpCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "aiwarp";
        public string Description => "Teleports you (AI) to predefined areas on the map.";

        public string Help =>
            "aiwarp <location>\nLocations you can teleport to are predefined by the map. " +
            "You can specify '?' as location to get a list of valid locations.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null)
            {
                shell.WriteLine("Only players can use this command");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteLine("Expected a single argument.");
                return;
            }

            var location = args[0];
            if (location == "?")
            {
                var locations = string.Join(", ", GetWarpPointNames());

                shell.WriteLine(locations);
            }
            else
            {
                if (player.Status != SessionStatus.InGame || player.AttachedEntity is not { Valid: true } playerEntity)
                {
                    shell.WriteLine("You are not in-game!");
                    return;
                }

                if (!_entManager.HasComponent<StationAiHeldComponent>(playerEntity))
                {
                    shell.WriteLine("You are not Station AI!");
                    return;
                }

                var currentMap = _entManager.GetComponent<TransformComponent>(playerEntity).MapID;
                var currentGrid = _entManager.GetComponent<TransformComponent>(playerEntity).GridUid;
                var currentOwner = _entManager.GetComponent<StationAiHeldComponent>(playerEntity).Owner;

                var xformSystem = _entManager.System<SharedTransformSystem>();

                var found = GetWarpPointByName(location)
                    .OrderBy(p => p.Item1, Comparer<EntityCoordinates>.Create((a, b) =>
                    {
                        // Sort so that warp points on the same grid/map are first.
                        // So if you have two maps loaded with the same warp points,
                        // it will prefer the warp points on the map you're currently on.
                        var aGrid = xformSystem.GetGrid(a);
                        var bGrid = xformSystem.GetGrid(b);

                        if (aGrid == bGrid)
                        {
                            return 0;
                        }

                        if (aGrid == currentGrid)
                        {
                            return -1;
                        }

                        if (bGrid == currentGrid)
                        {
                            return 1;
                        }

                        var mapA = xformSystem.GetMapId(a);
                        var mapB = xformSystem.GetMapId(b);

                        if (mapA == mapB)
                        {
                            return 0;
                        }

                        if (mapA == currentMap)
                        {
                            return -1;
                        }

                        if (mapB == currentMap)
                        {
                            return 1;
                        }

                        return 0;
                    }))
                    .FirstOrDefault();

                var (coords, follow) = found;

                if (coords.EntityId == EntityUid.Invalid)
                {
                    shell.WriteError("That location does not exist!");
                    return;
                }

                if (!TryGetCore(_entManager, currentOwner, out var core) || core.Comp?.RemoteEntity == null)
                    return;

                xformSystem.SetCoordinates(core.Comp.RemoteEntity.Value, coords);
                if (_entManager.TryGetComponent(playerEntity, out PhysicsComponent? physics))
                {
                    _entManager.System<SharedPhysicsSystem>().SetLinearVelocity(playerEntity, Vector2.Zero, body: physics);
                }
            }
        }

        private IEnumerable<string> GetWarpPointNames()
        {
            var aiVisionSystem = _entManager.System<StationAiVisionSystem>();
            List<string> points = new(_entManager.Count<WarpPointComponent>());

            var query = _entManager.AllEntityQueryEnumerator<WarpPointComponent, MetaDataComponent, MindComponent, TransformComponent>();

            while (query.MoveNext(out _, out var warp, out var meta, out var mind, out var xform))
            {
                var grid = xform.GridUid.Value;

                if (!_entManager.TryGetComponent<BroadphaseComponent>(grid, out var broadphase) ||
                    !_entManager.TryGetComponent<MapGridComponent>(grid, out var mapGrid)) {
                    continue;
                }

                var tile = mapGrid.TileIndicesFor(xform.Coordinates);
                var gridEntity = new Entity<BroadphaseComponent, MapGridComponent>(grid, broadphase, mapGrid);

                if (aiVisionSystem.IsAccessible(gridEntity, tile)) {
                    points.Add(warp.Location ?? meta.EntityName);
                }
            }

            points.Sort();
            return points;
        }

        private List<(EntityCoordinates, bool)> GetWarpPointByName(string name)
        {
            List<(EntityCoordinates, bool)> points = new();
            var query = _entManager.AllEntityQueryEnumerator<WarpPointComponent, MetaDataComponent, TransformComponent, MindComponent>();
            while (query.MoveNext(out var uid, out var warp, out var meta, out var xform, out var _))
            {
                if (name == (warp.Location ?? meta.EntityName))
                    points.Add((xform.Coordinates, warp.Follow));
            }

            return points;
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var options = new[] { "?" }.Concat(GetWarpPointNames());

                return CompletionResult.FromHintOptions(options, "<warp point | ?>");
            }

            return CompletionResult.Empty;
        }

        private bool TryGetCore(IEntityManager _entManager, EntityUid ent, out Entity<StationAiCoreComponent?> core)
        {
            var _containerSystem = _entManager.System<SharedContainerSystem>();
            if (!_containerSystem.TryGetContainingContainer((ent, null, null), out var container) ||
                container.ID != StationAiCoreComponent.Container ||
                !_entManager.TryGetComponent(container.Owner, out StationAiCoreComponent? coreComp) ||
                coreComp.RemoteEntity == null)
            {
                core = (EntityUid.Invalid, null);
                return false;
            }

            core = (container.Owner, coreComp);
            return true;
        }
    }
}
