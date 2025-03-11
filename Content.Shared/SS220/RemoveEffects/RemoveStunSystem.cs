using Content.Shared.Stunnable;

namespace Content.Shared.SS220.RemoveEffects;

public sealed class RemoveStunSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RemoveStunComponent>();

        while (query.MoveNext(out var target, out var removeEffectsComponent))
        {
            switch (removeEffectsComponent.Time)
            {
                case null:
                    continue;
                case <= 0:
                    RemCompDeferred<RemoveStunComponent>(target);
                    continue;
            }

            if (HasComp<StunnedComponent>(target))
                RemCompDeferred<StunnedComponent>(target);

            if (HasComp<KnockedDownComponent>(target))
                RemCompDeferred<KnockedDownComponent>(target);

            removeEffectsComponent.Time -= frameTime;
        }
    }
}
