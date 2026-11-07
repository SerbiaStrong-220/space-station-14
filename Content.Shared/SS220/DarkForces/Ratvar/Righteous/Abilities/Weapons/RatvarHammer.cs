using Content.Shared.Actions;

namespace Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Weapons;

public sealed partial class RatvarHammerKnockOffEvent : InstantActionEvent, IRatvarAbilityRelay
{
    public TimeSpan UseTime { get; set; } = TimeSpan.FromSeconds(5);
}
