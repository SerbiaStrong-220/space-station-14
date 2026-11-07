using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.EntityBlockDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class EntityBlockDamageComponent : Component
{
    [DataField]
    public float DamageCoefficient = 1.0f;

    [DataField]
    public DamageModifierSet? Modifiers;

    [DataField]
    public bool BlockAllTypesDamage;

    [DataField]
    public float? Duration;
}
