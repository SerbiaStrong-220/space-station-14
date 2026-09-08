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

    public static TimeSpan? GetClinicalDeathTimeRemaining(LimitationReviveComponent? revive, MobStateComponent? mobState)
    {
        if (revive == null || mobState == null)
            return null;

        if (mobState.CurrentState != MobState.Dead)
            return null;

        if (revive.DamageCountingTime is not { } countedTime)
            return TimeSpan.Zero;

        var timeLeft = revive.BeforeDamageDelay - countedTime;
        return timeLeft > TimeSpan.Zero ? timeLeft : TimeSpan.Zero;
    }
}
