using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.StaminaConvertArmor;

/// <summary>
/// Represents armor that interacts with stamina damage and status effects.
/// When equipped, it can convert part of incoming stamina damage into another damage type (e.g., electrical),
/// and block specified status effects from applying to the wearer.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StaminaConvertArmorComponent : Component
{
    /// <summary>
    /// The type of damage to apply when converting stamina damage.
    /// This should point to a damage prototype (e.g., Shock, Heat).
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType;

    /// <summary>
    /// The conversion ratio for stamina damage.
    /// For example, if set to 0.3, 30% of stamina damage will be redirected as configured damage.
    /// </summary>
    [DataField]
    public float DamageCoefficient;

    [DataField]
    public List<string> IgnoredEffects = new();

    /// <summary>
    /// The entity wearing this armor, set when the item is equipped.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public NetEntity? User;
}
