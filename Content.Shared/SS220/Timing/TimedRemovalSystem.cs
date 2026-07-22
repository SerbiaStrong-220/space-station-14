// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.Timing;

[ByRefEvent]
public record struct ComponentTimedRemovalExpiredEvent;
public sealed partial class ComponentTimedRemovalSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ComponentTimedRemovalComponent>();
        while (query.MoveNext(out var uid, out var timer))
        {
            if (_timing.CurTime < timer.EndTime)
                continue;

            var ev = new ComponentTimedRemovalExpiredEvent();
            RaiseLocalEvent(uid, ref ev);

            RemCompDeferred<ComponentTimedRemovalComponent>(uid);
        }
    }

    public void StartTimer(EntityUid uid, TimeSpan duration)
    {
        var timer = EnsureComp<ComponentTimedRemovalComponent>(uid);
        timer.EndTime = _timing.CurTime + duration;
        Dirty(uid, timer);
    }
}

