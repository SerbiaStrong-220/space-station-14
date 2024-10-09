using Content.Server.Station.Components;
using Content.Server.Station.Systems;

namespace Content.Server.SS220.Utils;

public sealed class StationUtils : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool IsOnMainStationGrid(EntityUid uid)
    {
        var xform = Transform(uid);
        var station = _station.GetOwningStation(uid, xform);
        if (!station.HasValue)
            return false;

        if (!TryComp<StationDataComponent>(station.Value, out var stationData))
            return false;

        if (_station.GetLargestGrid(stationData) is not { } grid)
            return false;

        return xform.GridUid == grid;
    }
}
