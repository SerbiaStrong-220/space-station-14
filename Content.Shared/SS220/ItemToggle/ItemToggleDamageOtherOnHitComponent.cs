using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ItemToggle;

/// <summary>
/// This is used for change damage for activating weapon
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemToggleDamageOtherOnHitComponent : Component
{
    /// <summary>
    ///     Damage done by this item when activated.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public DamageSpecifier? ActivatedDamage;

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public DamageSpecifier? DeactivatedDamage;
}
