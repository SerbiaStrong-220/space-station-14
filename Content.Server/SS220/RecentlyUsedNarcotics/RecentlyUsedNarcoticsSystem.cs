using Content.Shared.SS220.Narcotics;
using Robust.Shared.Timing;

namespace Content.Server.SS220.RecentlyUsedNarcotics;

public sealed class RecentlyUsedNarcoticsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetabolizeNarcoticEvent>(OnMetabolizeNarcotic);
    }

    private void OnMetabolizeNarcotic(ref MetabolizeNarcoticEvent args)
    {
        if (args.Handled)
            return;

        var narcoticsComp = EnsureComp<RecentlyUsedNarcoticsComponent>(args.Body);
        narcoticsComp.LastTimeUsedNarcotics = _gameTiming.CurTime;
        narcoticsComp.TimeRemoveNarcoticsFromBlood = narcoticsComp.LastTimeUsedNarcotics + narcoticsComp.AddTimeForOneUse;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RecentlyUsedNarcoticsComponent>();

        while (query.MoveNext(out var user, out var narcotics))
        {
            if (_gameTiming.CurTime < narcotics.TimeRemoveNarcoticsFromBlood)
                continue;

            RemCompDeferred<RecentlyUsedNarcoticsComponent>(user);
        }
    }
}
