using System.Numerics;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain;

public sealed partial class ChaplainSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string ChaplainForceWallNarsi = "ChaplainForceWallNarsi";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChaplainForceWallDefault = "ChaplainForceWallDefault";

    private void SpawnBarriers(EntityUid chaplain, int range, string prototype)
    {
        var transform = Transform(chaplain);
        if (transform.MapUid is not { } map)
            return;

        // Work in grid-local coordinates so rotated and moving grids retain a closed barrier.
        var parent = transform.GridUid ?? map;
        var tileSize = TryComp<MapGridComponent>(parent, out var grid) ? grid.TileSize : 1f;
        var position = _transformSystem.WithEntityId(transform.Coordinates, parent).Position;
        var tile = new Vector2(MathF.Floor(position.X / tileSize), MathF.Floor(position.Y / tileSize));

        var box = new Box2(tile - new Vector2(range), tile + new Vector2(range));
        var box2IEdgeEnumerator = new Box2EdgeEnumerator(box, true);

        while (box2IEdgeEnumerator.MoveNext(out var index))
        {
            Spawn(prototype, new EntityCoordinates(parent, (index + new Vector2(0.5f)) * tileSize));
        }
    }
}
