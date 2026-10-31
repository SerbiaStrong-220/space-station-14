namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Armor;

[RegisterComponent]
public sealed partial class RatvarCuirassComponent : Component
{
    [DataField]
    public bool IsReflection;

    [DataField]
    public bool IsAbsorb;

    [DataField]
    public int AbsorbCount;

    [DataField]
    public bool IsHardenPlates;
}
