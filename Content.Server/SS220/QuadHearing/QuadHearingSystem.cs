using Content.Shared.SS220.QuadHearing;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.SS220.QuadHearing;

public sealed class QuadHearingSystem : SharedQuadHearingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MergeRangeSquared = 1f;
    private static readonly TimeSpan LifeTime = TimeSpan.FromSeconds(3);

    private readonly Dictionary<string, List<VisualsData>> _visualsData = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var datas in _visualsData.Values)
        {
            var toRem = new List<VisualsData>();
            foreach (var data in datas)
                if (_timing.CurTime >= data.EndTime)
                    toRem.Add(data);

            foreach (var value in toRem)
                datas.Remove(value);
        }
    }

    public void RegisterTarget(EntityCoordinates coords, string id)
    {
        if (!_visualsData.TryGetValue(id, out var datas))
        {
            datas = [];
            _visualsData[id] = datas;
        }

        var data = datas
            .Find(x => x.Coords.EntityId == coords.EntityId && (x.Coords.Position - coords.Position).LengthSquared() <= MergeRangeSquared);

        data ??= new VisualsData() { Coords = coords };
        data.EndTime = _timing.CurTime + LifeTime;
    }

    private sealed class VisualsData
    {
        public required EntityCoordinates Coords;

        public TimeSpan EndTime;
    }
}
