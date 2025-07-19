// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Shuttles.UI;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Client.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public List<ProjectileDrawInfo> ProjectilesToDraw = [];
    public List<HitscanDrawInfo> HitscansToDraw = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShuttleNavInfoUpdateProjectilesMessage>(OnUpdateProjectiles);
        SubscribeNetworkEvent<ShuttleNavInfoAddHitscanMessage>(OnAddHitscan);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDelete = new List<HitscanDrawInfo>();
        foreach (var info in HitscansToDraw)
        {
            if (info.EndTime <= _timing.CurTime)
                toDelete.Add(info);
        }

        foreach (var info in toDelete)
            HitscansToDraw.Remove(info);
    }

    private void OnUpdateProjectiles(ShuttleNavInfoUpdateProjectilesMessage msg)
    {
        ProjectilesToDraw = [.. msg.List.Select(x => new ProjectileDrawInfo(x.CurCoordinates, x.Info))];
    }

    private void OnAddHitscan(ShuttleNavInfoAddHitscanMessage msg)
    {
        AddHitscan(msg.FromCoordinated, msg.ToCoordinated, msg.Info);
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Enabled)
            return;

        HitscansToDraw.Add(new HitscanDrawInfo(fromCoordinates, toCoordinates, info.Color, info.Width, info.AnimationLength, _timing.CurTime + info.AnimationLength));
    }

    public struct ProjectileDrawInfo(MapCoordinates curCoordinates, Color color, float radius)
    {
        public MapCoordinates CurCoordinates = curCoordinates;
        public Color Color = color;
        public float Radius = radius;

        public ProjectileDrawInfo(MapCoordinates curCoordinate, ShuttleNavProjectileInfo info) : this(curCoordinate, info.Color, info.Radius) { }
    }

    public struct HitscanDrawInfo(
        MapCoordinates fromCoordinates,
        MapCoordinates toCoordinates,
        Color color,
        float width,
        TimeSpan animationLength,
        TimeSpan endTime)
    {
        public MapCoordinates FromCoordinates = fromCoordinates;
        public MapCoordinates ToCoordinates = toCoordinates;
        public Color Color = color;
        public float Width = width;
        public TimeSpan AnimationLength = animationLength;
        public TimeSpan EndTime = endTime;
    }
}
