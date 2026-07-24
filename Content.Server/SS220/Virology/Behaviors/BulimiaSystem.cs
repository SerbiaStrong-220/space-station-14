// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical;
using Content.Shared.Nutrition;
using Content.Shared.SS220.Virology.Behaviors;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class BulimiaSystem : EntitySystem
{
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BulimiaComponent, IngestingEvent>(OnEating);
    }

    private void OnEating(Entity<BulimiaComponent> ent, ref IngestingEvent args)
    {
        ent.Comp.VomitAt = _timing.CurTime + ent.Comp.Delay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BulimiaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.VomitAt is not { } at || _timing.CurTime < at)
                continue;

            comp.VomitAt = null;
            _vomit.Vomit(uid);
        }
    }
}
