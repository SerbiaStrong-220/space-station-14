using Content.Shared.Damage;
using Robust.Shared.GameStates;

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
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? ActivatedDamage = null;

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? DeactivatedDamage = null;
}
