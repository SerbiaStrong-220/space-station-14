using Content.Shared.Actions;

namespace Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Weapons;

public sealed partial class RatvarSpearElectricalTouchEvent : InstantActionEvent, IRatvarAbilityRelay
{
    public TimeSpan UseTime { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed partial class RatvarSpearConfusionEvent : InstantActionEvent, IRatvarAbilityRelay
{
    public TimeSpan UseTime { get; set; } = TimeSpan.FromSeconds(5);
}
