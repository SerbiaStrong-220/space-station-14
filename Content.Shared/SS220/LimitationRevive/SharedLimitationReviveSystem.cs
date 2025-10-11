// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public abstract class SharedLimitationReviveSystem : EntitySystem
{
    public virtual void IncreaseTimer(EntityUid ent, TimeSpan addTime) { }
}
