using Content.Shared.Mobs.Systems;

namespace Content.Shared.SS220.ModifyThreshold;

public sealed class ModifyThresholdSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ModifyThresholdComponent>();

        while (query.MoveNext(out var target, out var component))
        {
            component.Duration -= frameTime;

            if (component.Duration <= 0)
            {
                foreach (var (value, state) in component.OldThresholds)
                {
                    _mobThreshold.SetMobStateThreshold(target, value, state);
                }

                RemCompDeferred<ModifyThresholdComponent>(target);
            }

            if (component.IsChanged)
                continue;

            foreach (var (value, state) in component.NewThresholds)
            {
                _mobThreshold.SetMobStateThreshold(target, value, state);
            }

            component.IsChanged = true;
        }
    }
}
