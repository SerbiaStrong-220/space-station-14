// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.Standing;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class NoodleArmsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<NoodleArmsComponent, PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var comp, out var holder))
        {
            if (!holder.ActivePathologies.TryGetValue(comp.Pathology, out var data))
                continue;

            switch (data.Level)
            {
                case 0:
                    comp.NextSpasm ??= _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
                    if (_timing.CurTime >= comp.NextSpasm)
                    {
                        DropAll(uid);
                        comp.NextSpasm = _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
                    }
                    break;

                case >= 1:
                    DropAll(uid);
                    break;
            }
        }
    }

    private void DropAll(EntityUid uid)
    {
        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
