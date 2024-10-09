using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment;

[RegisterComponent]
public sealed partial class RatvarSlabComponent : Component
{
    [DataField]
    public DamageSpecifier HealingHumanoidDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Blunt", -30},
            {"Burn", -30}
        }
    };


    [DataField]
    public DamageSpecifier HealingMarauderDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Blunt", 50}
        }
    };

    [DataField]
    public bool HidingStructure;
}
