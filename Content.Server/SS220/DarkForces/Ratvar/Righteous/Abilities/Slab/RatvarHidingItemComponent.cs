namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Slab;

[RegisterComponent]
public sealed partial class RatvarHidingItemComponent : Component
{
    [DataField]
    public EntityUid? OriginalItem;
}
