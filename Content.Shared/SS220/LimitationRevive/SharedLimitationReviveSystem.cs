// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public abstract class SharedLimitationReviveSystem : EntitySystem
{
    public virtual void IncreaseTimer(EntityUid ent, TimeSpan addTime) { }

    public TimeSpan? GetClinicalDeathTimeRemaining(Entity<LimitationReviveComponent?, MobStateComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        if (ent.Comp2.CurrentState != MobState.Dead)
            return null;

        if (ent.Comp1.DamageCountingTime is not { } countedTime)
            return TimeSpan.Zero;

        var timeLeft = ent.Comp1.BeforeDamageDelay - countedTime;
        return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
    }
}
