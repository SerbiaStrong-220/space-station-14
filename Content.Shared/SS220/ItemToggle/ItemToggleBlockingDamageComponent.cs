using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ItemToggle;

/// <summary>
/// This is used for changing blocking damage while item not activated
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ItemToggleBlockingDamageComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? OriginalActiveModifier;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? OriginalPassiveModifier;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? DeactivatedActiveModifier;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet? DeactivatedPassiveModifier;

    [DataField]
    [AutoNetworkedField]
    public float OriginalActivatedFraction;

    [DataField]
    [AutoNetworkedField]
    public float OriginalDeactivatedFraction;

    [DataField]
    [AutoNetworkedField]
    public float DeactivatedActiveFraction;

    [DataField]
    [AutoNetworkedField]
    public float DeactivatedPassiveFraction;
}
