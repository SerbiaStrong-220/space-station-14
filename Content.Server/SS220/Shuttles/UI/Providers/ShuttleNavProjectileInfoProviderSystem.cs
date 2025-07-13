using Content.Server.SS220.Shuttles.UI.Components;
using Content.Shared.SS220.Shuttles.UI;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.Shuttles.UI.Providers;

public sealed class ShuttleNavProjectileInfoProviderSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ShuttleNavInfoSystem _shuttleNavInfo = default!;

    private ShuttleNavProjectilesInfo _info = default!;

    public override void Initialize()
    {
        base.Initialize();

        _info = _shuttleNavInfo.EnsureInfo<ShuttleNavProjectilesInfo>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _info.Infos.Clear();
        var query = EntityQueryEnumerator<ShuttleNavProjectileInfoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var info = new ShuttleNavProjectilesInfo.ProjectileInfo
            {
                Coordinates = _transform.GetMapCoordinates(uid),
                CircleRadius = comp.Size,
                Color = comp.Color
            };
            _info.Infos.Add(info);
        }

        _shuttleNavInfo.UpdateClients();
    }
}
