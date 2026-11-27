using Content.Shared.Chemistry;
using Content.Shared.FixedPoint;

namespace Content.Server.SS220.DarkForces.Saint.Reagent.Events;

/**
 * Вызывается, если у сущности есть Flammable в Reactive компоненте
 */
public sealed class OnSaintWaterFlammableEvent : CancellableEntityEventArgs
{
    public EntityUid Target;
    public FixedPoint2 SaintWaterAmount;
    public ReactionMethod? ReactionMethod;

    public OnSaintWaterFlammableEvent(EntityUid target, FixedPoint2 saintWaterAmount, ReactionMethod? reactionMethod)
    {
        Target = target;
        SaintWaterAmount = saintWaterAmount;
        ReactionMethod = reactionMethod;
    }
}
