// SS220 Changeling
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    /// <summary>
    /// Checks a damage threshold without exposing mutable damage state or allocating a damage snapshot.
    /// </summary>
    public bool IsDamageAtLeast(Entity<DamageableComponent> ent, FixedPoint2 threshold)
    {
        return ent.Comp.TotalDamage >= threshold;
    }
}
