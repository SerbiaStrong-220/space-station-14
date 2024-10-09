using System.Numerics;
using System.Threading.Tasks;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain;

public sealed partial class ChaplainSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string ChaplainForceWallNarsi = "ChaplainForceWallNarsi";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChaplainForceWallDefault = "ChaplainForceWallDefault";

    private void InitializeForceWall()
    {
    }

    private async Task SpawnBarriers(EntityUid chaplain, int range, string prototype)
    {
        var transform = Transform(chaplain);
        var vector = _transformSystem.GetMapCoordinates(chaplain).Position;

        var xMin = vector.X - range;
        var xMax = vector.X + range;
        var yMin = vector.Y - range;
        var yMax = vector.Y + range;

        var box = new Box2(new Vector2(xMin, yMin), new Vector2(xMax, yMax));
        var box2IEdgeEnumerator = new Box2EdgeEnumerator(box, true);

        while (box2IEdgeEnumerator.MoveNext(out var index))
        {
            var entity = Spawn(prototype, new MapCoordinates(index, transform.MapID));
            _transformSystem.AttachToGridOrMap(entity);
        }
    }
}
