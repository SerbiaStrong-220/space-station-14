// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeGreatFireballLavaTrailSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeGreatFireballLavaTrailComponent, StartCollideEvent>(OnCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AshDrakeGreatFireballLavaTrailComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.TimeSinceLastLava += frameTime;
            if (comp.TimeSinceLastLava < AshDrakeGreatFireballLavaTrailComponent.LavaTrailInterval)
                continue;

            comp.TimeSinceLastLava = 0f;
            SpawnLavaAt(uid);
        }
    }

    private void OnCollide(EntityUid uid, AshDrakeGreatFireballLavaTrailComponent comp, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard || Deleted(uid))
            return;

        var coords = Transform(uid).Coordinates;
        Spawn("AshDrakeGreatFireballExplosion", coords);
        Del(uid);
    }

    private void SpawnLavaAt(EntityUid uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return;

        var pos = _transform.GetWorldPosition(xform);
        var center = grid.TileIndicesFor(new MapCoordinates(pos, xform.MapID));

        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            var offset = new Vector2i(center.X + x, center.Y + y);
            Spawn("AshDrakeFlyLava", grid.GridTileToLocal(offset));
        }
    }
}
