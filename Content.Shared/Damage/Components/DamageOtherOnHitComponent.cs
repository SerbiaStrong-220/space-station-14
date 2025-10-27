using Content.Shared.Damage.Systems;
using Content.Shared.SS220.ItemToggle;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Makes this entity deal damage when thrown at something.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedDamageOtherOnHitSystem), typeof(ItemToggleDamageOtherOnHitSystem))] //ss220 fix stunbaton
public sealed partial class DamageOtherOnHitComponent : Component
{
    /// <summary>
    /// Whether to ignore damage modifiers.
    /// </summary>
    [DataField]
    public bool IgnoreResistances = false;

    /// <summary>
    /// The damage amount to deal on hit.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

}
