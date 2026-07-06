// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Sleep;
using Content.Shared.SS220.Virology;
using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusHypersomniaSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
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

        var query = EntityQueryEnumerator<VirusHypersomniaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Anywhere)
            {
                if (_timing.CurTime >= (comp.NextSleep ?? TimeSpan.Zero)
                    && VirusEffectConditions.IsRecumbent(uid, EntityManager)
                    && !_statusEffects.HasEffectComp<ForcedSleepingStatusEffectComponent>(uid))
                {
                    Sleep((uid, comp));
                    comp.NextSleep = _timing.CurTime + comp.SleepDuration + comp.WakeGrace;
                }

                continue;
            }

            comp.NextSleep ??= _timing.CurTime + _random.Next(comp.MinInterval, comp.MaxInterval);
            if (_timing.CurTime >= comp.NextSleep)
            {
                Sleep((uid, comp));
                comp.NextSleep = _timing.CurTime + comp.SleepDuration + _random.Next(comp.MinInterval, comp.MaxInterval);
            }
        }
    }

    private void Sleep(Entity<VirusHypersomniaComponent> ent)
    {
        _statusEffects.TryAddStatusEffectDuration(ent.Owner, SleepingSystem.StatusEffectForcedSleeping, ent.Comp.SleepDuration);
    }
}
