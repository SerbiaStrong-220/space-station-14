// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed partial class AshDrakeGreatFireballLavaTrailSystem : EntitySystem
{
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AshDrakeGreatFireballLavaTrailComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!TryComp(uid, out TransformComponent? xform))
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
        if (xform.GridUid is not { Valid: true } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComponent))
            return;

        var pos = _transform.GetWorldPosition(xform);
        var center = _map.TileIndicesFor(gridUid, gridComponent, new MapCoordinates(pos, xform.MapID));

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var coordinates = _map.GridTileToLocal(gridUid, gridComponent, new Vector2i(center.X + x, center.Y + y));
                Spawn("AshDrakeFlyLava", coordinates);
            }
        }
    }
}
