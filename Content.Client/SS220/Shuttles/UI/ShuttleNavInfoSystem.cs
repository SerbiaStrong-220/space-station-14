using Content.Shared.SS220.Shuttles.UI;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public List<(MapCoordinates FromCoordinates, MapCoordinates ToCoordinates, ShuttleNavHitscanInfo Info, TimeSpan EndTime)> HitscansToDraw = [];
    public List<(MapCoordinates CurCoordinate, ShuttleNavProjectileInfo Info)> ProjectilesToDraw = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShuttleNavInfoUpdateProjectilesMessage>(OnUpdateProjectiles);
        SubscribeNetworkEvent<ShuttleNavInfoAddHitscanMessage>(OnAddHitscan);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDelete = new List<(MapCoordinates FromCoordinates, MapCoordinates ToCoordinates, ShuttleNavHitscanInfo Info, TimeSpan EndTime)>();
        foreach (var value in HitscansToDraw)
        {
            if (value.EndTime <= _timing.CurTime)
                toDelete.Add(value);
        }

        foreach (var value in toDelete)
            HitscansToDraw.Remove(value);
    }

    private void OnUpdateProjectiles(ShuttleNavInfoUpdateProjectilesMessage msg)
    {
        ProjectilesToDraw = [.. msg.List];
    }

    private void OnAddHitscan(ShuttleNavInfoAddHitscanMessage msg)
    {
        AddHitscan(msg.FromCoordinated, msg.ToCoordinated, msg.Info);
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Show)
            return;

        HitscansToDraw.Add((fromCoordinates, toCoordinates, info, _timing.CurTime + info.AnimationLength));
    }
}
