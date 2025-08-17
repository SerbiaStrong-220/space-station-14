// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Shuttles.UI;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Client.SS220.Shuttles.UI;

public sealed class ShuttleNavInfoSystem : SharedShuttleNavInfoSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<Type, List<IDrawInfo>> _toDraw = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShuttleNavInfoAddHitscanMessage>(OnAddHitscan);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var toDelete = new List<HitscanDrawInfo>();
        foreach (var info in GetDrawInfo<HitscanDrawInfo>())
        {
            if (info.EndTime <= _timing.CurTime)
                toDelete.Add(info);
        }

        foreach (var info in toDelete)
            RemoveDrawInfo(info);
    }

    private void OnAddHitscan(ShuttleNavInfoAddHitscanMessage msg)
    {
        AddHitscan(msg.FromCoordinated, msg.ToCoordinated, msg.Info);
    }

    public override void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info)
    {
        if (!info.Enabled)
            return;

        var drawInfo = new HitscanDrawInfo(fromCoordinates, toCoordinates, info.Color, info.Width, info.AnimationLength, _timing.CurTime + info.AnimationLength);
        AddDrawInfo(drawInfo);
    }

    public IEnumerable<T> GetDrawInfo<T>() where T : IDrawInfo
    {
        return GetToDrawList<T>().Select(x => (T)x);
    }

    public void AddDrawInfo<T>(T info) where T : IDrawInfo
    {
        var list = GetToDrawList<T>();
        list.Add(info);
    }

    public bool RemoveDrawInfo<T>(T info) where T : IDrawInfo
    {
        var list = GetToDrawList<T>();
        return list.Remove(info);
    }

    public void ClearDrawInfo<T>() where T : IDrawInfo
    {
        var list = GetToDrawList<T>();
        list.Clear();
    }

    private List<IDrawInfo> GetToDrawList<T>() where T : IDrawInfo
    {
        if (!_toDraw.TryGetValue(typeof(T), out var list))
        {
            list = [];
            _toDraw.Add(typeof(T), list);
        }

        return list;
    }

    public interface IDrawInfo { }

    public struct HitscanDrawInfo(
        MapCoordinates fromCoordinates,
        MapCoordinates toCoordinates,
        Color color,
        float width,
        TimeSpan animationLength,
        TimeSpan endTime) : IDrawInfo
    {
        public MapCoordinates FromCoordinates = fromCoordinates;
        public MapCoordinates ToCoordinates = toCoordinates;
        public Color Color = color;
        public float Width = width;
        public TimeSpan AnimationLength = animationLength;
        public TimeSpan EndTime = endTime;
    }
}
