// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class BonusDamageMartialArtEffectSystem : BaseMartialArtEffectSystem<BonusDamageMartialArtEffect, BonusDamageMartialArtEffectComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BonusDamageMartialArtEffectComponent, MeleeHitEvent>(OnMelee);
    }

    private void OnMelee(EntityUid uid, BonusDamageMartialArtEffectComponent artist, MeleeHitEvent ev)
    {
        if (!TryEffect(uid, out var effect))
            return;

        if (!effect.WithWeapons && ev.Weapon != ev.User)
            return;

        var bonusDamage = new DamageSpecifier
        {
            DamageDict = ev.BaseDamage.DamageDict.Select((pair) => (pair.Key, FixedPoint2.Zero)).ToDictionary()
        };

        if (effect.BonusDamage != 0)
            bonusDamage.AddDamageEvenly(effect.BonusDamage);

        if (TryComp<StandingStateComponent>(uid, out var standing))
        {
            if (effect.BonusDamageTargetDown != 0 && !standing.Standing)
                bonusDamage.AddDamageEvenly(effect.BonusDamageTargetDown);

            if (effect.BonusDamageTargetUp != 0 && standing.Standing)
                bonusDamage.AddDamageEvenly(effect.BonusDamageTargetUp);
        }

        ev.BonusDamage += bonusDamage;
    }
}

public sealed partial class BonusDamageMartialArtEffect : MartialArtEffectBase<BonusDamageMartialArtEffect>
{
    /// <summary>
    /// Evenly adds specifier amount of damage to all damage types in melee
    /// </summary>
    [DataField]
    public float BonusDamage = 0f;

    /// The same as bonus damage but applied only if target is down
    [DataField]
    public float BonusDamageTargetDown = 0f;

    /// The same as bonus damage but applied only if target is up
    [DataField]
    public float BonusDamageTargetUp = 0f;

    /// <summary>
    /// Whether or not the modifiers should be applied when weapon is used
    /// </summary>
    [DataField]
    public bool WithWeapons = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BonusDamageMartialArtEffectComponent : Component;
