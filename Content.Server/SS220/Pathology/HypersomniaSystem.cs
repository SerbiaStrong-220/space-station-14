// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class HypersomniaSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private StandingStateSystem _standing = default!;
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

        var query = EntityQueryEnumerator<HypersomniaComponent, PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var comp, out var holder))
        {
            if (!holder.ActivePathologies.TryGetValue(comp.Pathology, out var data))
                continue;

            switch (data.Level)
            {
                case 1:
                    if (_timing.CurTime >= (comp.NextSleep ?? TimeSpan.Zero)
                        && IsRecumbent(uid)
                        && !_statusEffects.HasEffectComp<ForcedSleepingStatusEffectComponent>(uid))
                    {
                        Sleep((uid, comp));
                        comp.NextSleep = _timing.CurTime + comp.SleepDuration + comp.WakeGrace;
                    }
                    break;

                case >= 2:
                    comp.NextSleep ??= _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
                    if (_timing.CurTime >= comp.NextSleep)
                    {
                        Sleep((uid, comp));
                        comp.NextSleep = _timing.CurTime + comp.SleepDuration + _random.Next(comp.MinInterval, comp.MaxInterval);
                    }
                    break;
            }
        }
    }

    private void Sleep(Entity<HypersomniaComponent> ent)
    {
        _statusEffects.TryAddStatusEffectDuration(ent.Owner, SleepingSystem.StatusEffectForcedSleeping, ent.Comp.SleepDuration);
    }

    private bool IsRecumbent(EntityUid uid)
    {
        if (_standing.IsDown(uid))
            return true;

        return TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled;
    }
}
