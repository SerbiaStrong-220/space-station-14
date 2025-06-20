// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Damage.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class DamageOnStunContactComponent : Component
{
    /// <summary>
    /// The damage done those stunned by <see cref="StunsOnContactsComponent"/>
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// The damage done those (with special component)  stunned by <see cref="StunsOnContactsComponent"/>
    /// </summary>
    [DataField]
    public DamageSpecifier? SpecialDamage;

    /// <summary>
    /// Special damage will be applied if whitelist condition matches
    /// </summary>
    [DataField]
    public EntityWhitelist? SpecialDamageWhitelist;
}
