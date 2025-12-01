// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts.Effects;

public sealed partial class LegSweepMartialArtEffectSystem : BaseMartialArtEffectSystem<LegSweepMartialArtEffect, LegSweepMartialArtEffectComponent>
{
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegSweepMartialArtEffectComponent, MeleeHitEvent>(OnMelee);
    }

    private void OnMelee(EntityUid uid, LegSweepMartialArtEffectComponent artist, MeleeHitEvent ev)
    {
        if (!TryEffect(uid, out var effect))
            return;

        if (!TryComp<StandingStateComponent>(uid, out var standing) || standing.Standing)
            return;

        if (!effect.WithWeapons && ev.Weapon != ev.User)
            return;

        if (ev.HitEntities.FirstOrNull() is not { } target)
            return;

        var newDamage = new DamageSpecifier(ev.BaseDamage);

        if (effect.BonusDamage != 0)
            newDamage.AddDamageEvenly(effect.BonusDamage);

        if (TryComp<StandingStateComponent>(target, out var targetStanding))
        {
            if (effect.BonusDamageTargetDown != 0 && !targetStanding.Standing)
                newDamage.AddDamageEvenly(effect.BonusDamageTargetDown);

            if (effect.BonusDamageTargetUp != 0 && targetStanding.Standing)
                newDamage.AddDamageEvenly(effect.BonusDamageTargetUp);

            if (effect.KnockdownTime > TimeSpan.Zero)
                _stun.TryKnockdown(target, effect.KnockdownTime, effect.KnockdownRefresh);
        }
        ev.BonusDamage += newDamage - ev.BaseDamage;
    }
}

/// <summary>
/// Bonus damage but applies only when artist is laying down
/// </summary>
public sealed partial class LegSweepMartialArtEffect : MartialArtEffectBase<LegSweepMartialArtEffect>
{
    [DataField]
    public float BonusDamage = 0f;

    /// <summary>
    /// The same as bonus damage but applied only if target is down
    /// </summary>
    [DataField]
    public float BonusDamageTargetDown = 0f;

    /// <summary>
    /// The same as bonus damage but applied only if target is up
    /// </summary>
    [DataField]
    public float BonusDamageTargetUp = 0f;

    /// <summary>
    /// Whether or not the modifiers should be applied when weapon is used
    /// </summary>
    [DataField]
    public bool WithWeapons = false;

    /// <summary>
    /// Knocks down target on hit unless no time is specified
    /// </summary>
    [DataField]
    public TimeSpan? KnockdownTime;

    [DataField]
    public bool KnockdownRefresh = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class LegSweepMartialArtEffectComponent : Component;
