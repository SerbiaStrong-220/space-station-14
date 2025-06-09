﻿// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.EntityEffects;

/**
 * Событие прокидывается, когда святая вода выпита.
 * Может быть перехвачено системами или отменено
 */
public sealed class OnSaintWaterDrinkEvent : CancellableEntityEventArgs
{
    public EntityUid Target;
    public FixedPoint2 SaintWaterAmount;

    public OnSaintWaterDrinkEvent(EntityUid target, FixedPoint2 saintWaterAmount)
    {
        Target = target;
        SaintWaterAmount = saintWaterAmount;
    }
}
