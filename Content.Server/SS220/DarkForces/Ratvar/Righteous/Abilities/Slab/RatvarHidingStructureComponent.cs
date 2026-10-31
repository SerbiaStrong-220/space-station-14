namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Slab;

[RegisterComponent]
public sealed partial class RatvarHidingStructureComponent : Component
{
    [DataField]
    public EntityUid? OriginalStructure;

    [DataField]
    public EntityUid? HidingSlab;
}
