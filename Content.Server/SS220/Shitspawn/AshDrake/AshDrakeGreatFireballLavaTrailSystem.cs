// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeGreatFireballLavaTrailSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AshDrakeGreatFireballLavaTrailComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp<TransformComponent>(uid, out var xform))
                continue;

            comp.TimeSinceLastLava += frameTime;
            if (comp.TimeSinceLastLava < AshDrakeGreatFireballLavaTrailComponent.LavaTrailInterval)
                continue;

            comp.TimeSinceLastLava = 0f;
            SpawnLavaAt(uid, xform);
        }
    }

    private void SpawnLavaAt(EntityUid uid, TransformComponent xform)
    {
        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = _transform.GetWorldPosition(xform);
        var center = grid.TileIndicesFor(new MapCoordinates(pos, xform.MapID));

        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            Spawn("AshDrakeFlyLava", grid.GridTileToLocal(new Vector2i(center.X + x, center.Y + y)));
        }
    }
}
