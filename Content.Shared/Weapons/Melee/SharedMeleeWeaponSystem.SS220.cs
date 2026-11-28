// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Weapons.Melee;

public abstract partial class SharedMeleeWeaponSystem
{
    /// <summary>
    /// Spends an attack of the weapon outside of a melee attack, e.g. a hit by a failed surgery step.
    /// The attack can be cancelled the same way as a melee one (e.g. a weapon that needs to be wielded).
    /// </summary>
    /// <returns>False if the weapon is still on cooldown or the attack is cancelled</returns>
    public bool TryUseAttack(Entity<MeleeWeaponComponent> weapon, EntityUid user)
    {
        var curTime = Timing.CurTime;
        if (curTime < weapon.Comp.NextAttack)
            return false;

        var userEv = new AttemptMeleeUserEvent(weapon);
        RaiseLocalEvent(user, ref userEv);
        if (userEv.Cancelled)
            return false;

        var weaponEv = new AttemptMeleeEvent(user);
        RaiseLocalEvent(weapon, ref weaponEv);
        if (weaponEv.Cancelled)
            return false;

        weapon.Comp.NextAttack = curTime + TimeSpan.FromSeconds(1f / GetAttackRate(weapon, user, weapon.Comp));
        DirtyField(weapon, weapon.Comp, nameof(MeleeWeaponComponent.NextAttack));
        return true;
    }
}
