// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg;

public sealed class CultYoggCleansedSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        //ToDo set time before it deviate
        var query = EntityQueryEnumerator<CultYoggCleansedComponent>();
        while (query.MoveNext(out var uid, out var cleansedComp))
        {
            if (_timing.CurTime < cleansedComp.CleansingDecayEventTime)
                continue;

            RemComp<CultYoggCleansedComponent>(uid);
        }
    }
}
