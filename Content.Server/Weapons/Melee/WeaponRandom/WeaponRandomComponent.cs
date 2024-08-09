using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Weapons.Melee.WeaponRandom;

[RegisterComponent]
internal sealed partial class WeaponRandomComponent : Component
{

    /// <summary>
    /// Amount of damage that will be caused. This is specified in the yaml.
    /// </summary>
    [DataField("damageBonus")]
    public DamageSpecifier DamageBonus = new();

    /// <summary>
    /// Chance for the damage bonus to occur (1 = 100%).
    /// </summary>
    [DataField("randomDamageChance"), ViewVariables(VVAccess.ReadWrite)] // SS220 add DataField
    public float RandomDamageChance = 0.00001f;

    /// <summary>
    /// Sound effect to play when the damage bonus occurs.
    /// </summary>
    [DataField("damageSound")]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

}
