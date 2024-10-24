// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.WristWatch;

public abstract class SharedWristWatchSystem<TComp> : EntitySystem where TComp : SharedWristWatchComponent
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TComp, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<TComp> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        args.PushMarkup(Loc.GetString("comp-clocks-time-description", ("time", stationTime.ToString("hh\\:mm\\:ss"))));
    }
}
