// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Projectiles;
using Content.Shared.SS220.Shuttles.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateProjectiles();
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Enabled)
            return;

        var ev = new ShuttleNavInfoAddHitscanMessage(fromCoordinates, toCoordinates, info);
        RaiseNetworkEvent(ev);
    }

    private void UpdateProjectiles()
    {
        var list = new List<(MapCoordinates, ShuttleNavProjectileInfo)>();
        var query = EntityQueryEnumerator<ProjectileComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ShuttleNavProjectileInfo is not { } info ||
                !info.Enabled)
                continue;

            list.Add((_transform.GetMapCoordinates(uid), info));
        }

        var ev = new ShuttleNavInfoUpdateProjectilesMessage(list);
        RaiseNetworkEvent(ev);
    }
}
