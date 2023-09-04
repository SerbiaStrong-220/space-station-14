using Content.Server.Stunnable.Components;
using Content.Shared.Damage;
using Content.Shared.SS220.Damage;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Stunnable;

public abstract class SharedStunbatonSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunbatonComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<StunbatonComponent, GetDamageOtherOnHitEvent>(OnGetDamageOnHit);
        SubscribeLocalEvent<StunbatonComponent, MeleeHitEvent>(OnGetMeeleHit);
    }

    private void OnGetDamageOnHit(EntityUid uid, StunbatonComponent component, GetDamageOtherOnHitEvent args)
    {
        if (!component.Activated)
        {
            args.Damage.DamageDict.Remove("Stamina");
            return;
        }

        args.Damage.DamageDict.Remove("Blunt");
    }

    private void OnGetMeeleHit(EntityUid uid, StunbatonComponent component, MeleeHitEvent args)
    {
        if (!component.Activated)
            return;

        if (!TryComp<MeleeWeaponComponent>(uid, out var meeleComp))
            return;

        args.HitSoundOverride = meeleComp.HitSound;
    }

    private void OnGetMeleeDamage(EntityUid uid, StunbatonComponent component, ref GetMeleeDamageEvent args)
    {
            if (!component.Activated)
                args.Damage.DamageDict.Remove("Stamina");
            else
                args.Damage.DamageDict.Remove("Blunt");

    }
}
