// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.ToggleBlocking;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Random;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem
{
    private void OnBlockUserCollide(Entity<AltBlockingUserComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, args.ProjectileRotation);
    }

    private void OnBlockThrownProjectile(Entity<AltBlockingUserComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, args.HitAngle);
    }

    private void OnBlockUserHitscan(Entity<AltBlockingUserComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, args.HitAngle);
    }

    private void OnBlockUserMeleeHit(Entity<AltBlockingUserComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        foreach (var item in ent.Comp.BlockingItemsShields)
        {
            if (!TryComp<AltBlockingComponent>(item, out var blockComp))
                continue;

            if (!IsCovered(args.HitAngle, blockComp.CoveredAngle, _transform.GetWorldRotation(ent.Owner)))
                continue;

            if (!TryGetNetEntity(item, out var netEnt))
                continue;

            if (TryComp<ToggleBlockingChanceComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                    continue;
            }

            if (!TryGetNetEntity(blockComp.User, out var NetUser))
                continue;

            if (!TryGetNetEntity(item, out var NetItem))
                continue;

            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_gameTiming.CurTick.Value, ((NetEntity)NetUser).Id, ((NetEntity)NetItem).Id });
            var rand = new System.Random(seed);

            if (ent.Comp.IsBlocking)
            {
                if (rand.Prob(blockComp.ActiveMeleeBlockProb))
                {
                    if (_net.IsServer)
                    {
                        _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                        _popupSystem.PopupEntity(Loc.GetString("block-shot"), ent.Owner);
                    }

                    args.CancelledHit = true;
                    args.blocker = netEnt;
                    return;
                }
            }

            else
            {
                if (rand.Prob(blockComp.MeleeBlockProb))
                {
                    if (_net.IsServer)
                    {
                        _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                        _popupSystem.PopupEntity(Loc.GetString("block-shot"), ent.Owner);
                    }
                    args.CancelledHit = true;
                    args.blocker = netEnt;
                    return;
                }
            }
        }
        return;
    }

    private void OnEquip(Entity<AltBlockingComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.User = args.User;
        Dirty(ent.Owner, ent.Comp);

        var userComp = EnsureComp<AltBlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(ent.Owner);

        if (TryComp<ArmorBlockComponent>(ent.Owner, out var armorComp))
            armorComp.User = args.User;

        Dirty(args.User, userComp);
    }

    private void OnUnequip(Entity<AltBlockingComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (_net.IsServer)
            StopBlockingHelper(ent, args.User);
    }

    private void OnDrop(Entity<AltBlockingComponent> ent, ref DroppedEvent args)
    {
        StopBlockingHelper(ent, args.User);
    }

    private void OnShutdown(Entity<AltBlockingComponent> ent, ref ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (ent.Comp.User != null)
            StopBlockingHelper(ent, ent.Comp.User.Value);
    }

    private bool TryBlock(List<EntityUid?> items, DamageSpecifier? damage, Entity<AltBlockingUserComponent> owner, Angle HitRotation)
    {
        foreach (var item in items)
        {
            if (!TryComp<AltBlockingComponent>(item, out var blockComp) || damage == null)
                continue;

            //if (!IsCovered(HitRotation, blockComp.CoveredAngle, _transform.GetWorldRotation(owner.Owner)))
            //    continue;

            if (TryComp<ToggleBlockingChanceComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                    continue;
            }

            if (blockComp.User == null)
                return false;

            var user = (EntityUid)blockComp.User;


            if (!TryGetNetEntity(blockComp.User, out var NetUser))
                continue;

            if (!TryGetNetEntity(item, out var NetItem))
                continue;

            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_gameTiming.CurTick.Value, ((NetEntity)NetUser).Id, ((NetEntity)NetItem).Id });
            var rand = new System.Random(seed);

            if (owner.Comp.IsBlocking)
            {
                if (rand.Prob(blockComp.ActiveRangeBlockProb))
                {
                    _damageable.TryChangeDamage((EntityUid)item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                    _popupSystem.PopupEntity(Loc.GetString("block-shot"), user);
                    return true;
                }
            }

            else
            {
                if (rand.Prob(blockComp.RangeBlockProb))
                {
                    _damageable.TryChangeDamage((EntityUid)item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                    _popupSystem.PopupEntity(Loc.GetString("block-shot"), user);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsCovered(Angle Incoming, Angle CoveredAngle, Angle UserRotation)
    {
        Incoming += new Angle(Math.PI);
        Incoming = Incoming.Reduced();
        UserRotation = UserRotation.Reduced();

        if (Math.Abs(Incoming.Theta - UserRotation.Theta) < CoveredAngle.Theta / 2)
            return true;

        return false;
    }
}
