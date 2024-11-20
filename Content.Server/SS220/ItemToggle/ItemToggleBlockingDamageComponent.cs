using Content.Shared.Damage;

namespace Content.Server.SS220.ItemToggle;

/// <summary>
/// This is used for changing blocking damage while item not activated
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleBlockingDamageComponent : Component
{
    [DataField]
    public DamageModifierSet? OriginalActiveModifier;

    [DataField]
    public DamageModifierSet? OriginalPassiveModifier;

    [DataField]
    public DamageModifierSet? DeactivatedActiveModifier;

    [DataField]
    public DamageModifierSet? DeactivatedPassiveModifier;

    [DataField]
    public float OriginalActivatedFraction;

    [DataField]
    public float OriginalDeactivatedFraction;

    [DataField]
    public float DeactivatedActiveFraction;

    [DataField]
    public float DeactivatedPassiveFraction;
}
