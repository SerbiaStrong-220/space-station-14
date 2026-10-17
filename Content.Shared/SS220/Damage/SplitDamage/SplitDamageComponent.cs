using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Damage.SplitDamage;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class SplitDamageComponent : Component
{
    [DataField]
    public DamageSpecifier WideDamage;

    [DataField]
    public DamageSpecifier PunchDamage;

    [DataField]
    public bool WideAttack;
}
