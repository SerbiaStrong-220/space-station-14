using System.Linq;
using Content.Shared.StatusEffect;

namespace Content.Shared.SS220.IgnoreStunEffect;

public sealed class IgnoreStunEffectSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<IgnoreStunEffectComponent>();

        while (query.MoveNext(out var target, out var ignoreComponent))
        {
            ignoreComponent.Time -= frameTime;
            if (ignoreComponent.Time > 0)
                continue;

            RemoveIgnore(target, ignoreComponent);
        }
    }

    private void RemoveIgnore(EntityUid target, IgnoreStunEffectComponent ignoreComponent)
    {
        if (!TryComp<StatusEffectsComponent>(target, out var effectsComponent))
            return;

        foreach (var effect in ignoreComponent.OriginalEffects
                     .Where(effect => !effectsComponent.AllowedEffects.Contains(effect)))
        {
            effectsComponent.AllowedEffects.Add(effect);
        }

        RemCompDeferred<IgnoreStunEffectComponent>(target);
    }
}
