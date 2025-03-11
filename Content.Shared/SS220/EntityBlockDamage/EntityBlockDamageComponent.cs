using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.EntityBlockDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class EntityBlockDamageComponent : Component
{
    [DataField]
    public DamageModifierSet? Modifiers;

    [DataField]
    public bool BlockAllDamage;

    [DataField]
    public FixedPoint2? BlockPercent;

    [DataField]
    public float? Duration;
}
