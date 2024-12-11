// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class AcsendingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcsendingComponent, ComponentInit>(SetupAcsending);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AcsendingComponent>();
        while (query.MoveNext(out var uid, out var acsend))
        {
            if (_timing.CurTime < acsend.AcsendingTime)
                continue;

            var ev = new CultYoggForceAscendingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void SetupAcsending(Entity<AcsendingComponent> uid, ref ComponentInit args)
    {
        uid.Comp.AcsendingTime = _timing.CurTime + uid.Comp.AcsendingInterval;
    }
}
