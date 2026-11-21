namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment.Items;

[RegisterComponent]
public sealed partial class RatvarShardComponent : Component
{
    [DataField]
    public string TileId = "FloorBrass";

    [DataField]
    public float ConvertRange = 3f;
}
