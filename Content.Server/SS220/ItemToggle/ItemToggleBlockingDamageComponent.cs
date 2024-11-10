using Content.Shared.Damage;

namespace Content.Server.SS220.ItemToggle;

/// <summary>
/// This is used for changing blocking damage while item not activated
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleBlockingDamageComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageModifierSet? OriginalActiveModifier;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageModifierSet? OriginalPassiveModifier;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageModifierSet? DeactivatedActiveModifier;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageModifierSet? DeactivatedPassiveModifier;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float OriginalActivatedFraction;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float OriginalDeactivatedFraction;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DeactivatedActiveFraction;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DeactivatedPassiveFraction;
}
