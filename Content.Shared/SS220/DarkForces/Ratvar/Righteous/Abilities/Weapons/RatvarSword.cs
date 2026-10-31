using Content.Shared.Actions;

namespace Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Weapons;

public sealed partial class RatvarSwordSwordsmanEvent : InstantActionEvent, IRatvarAbilityRelay
{
    public TimeSpan UseTime { get; set; } = TimeSpan.FromSeconds(8);
}


public sealed partial class RatvarSwordBloodshedEvent : InstantActionEvent, IRatvarAbilityRelay
{
    public TimeSpan UseTime { get; set; } = TimeSpan.FromSeconds(6);
}
