// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggPurifiedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggPurifiedComponent>();
        while (query.MoveNext(out var uid, out var cleansedComp))
        {
            if (_timing.CurTime < cleansedComp.PurifyingDecayEventTime)
                continue;

            RemComp<CultYoggPurifiedComponent>(uid);
        }
    }
}
