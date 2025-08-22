using Content.Shared.Damage;

namespace Content.Server.SS220.ItemToggle;

/// <summary>
/// This is used for change damage for activating weapon
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleDamageOtherOnHitComponent : Component
{
    /// <summary>
    ///     Damage done by this item when activated.
    /// </summary>
    [DataField]
    public DamageSpecifier? ActivatedDamage;

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [DataField]
    public DamageSpecifier? DeactivatedDamage;
}
