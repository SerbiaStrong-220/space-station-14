// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FCB.ToggleBlocking;
using Content.Shared.FCB.Weapons.Melee.Events;
using Content.Shared.FCB.Weapons.Ranged;
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Throwing;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Shared.FCB.AltBlocking;

public sealed partial class AltBlockingSystem
{

    private void OnBlockUserCollide(Entity<AltBlockingUserComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockThrownProjectile(Entity<AltBlockingUserComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    private void OnBlockUserHitscan(Entity<AltBlockingUserComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent.Comp);
    }

    //private void OnBlockUserHitscan(Entity<HitscanBasicDamageComponent> ent, ref AttemptHitscanRaycastFiredEvent args)
    //{
    //    if(TryComp<AltBlockingUserComponent>(args.Data.HitEntity, out var blockUserComp) && TryBlock(blockUserComp.BlockingItemsShields, ent.Comp.Damage, blockUserComp))
    //        return;
    //}

    private void OnBlockUserMeleeHit(Entity<AltBlockingUserComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        foreach (var item in ent.Comp.BlockingItemsShields)
        {
            if (!TryComp<AltBlockingComponent>(item, out var blockComp))
                return; 

            if (!TryGetNetEntity(item, out var netEnt))
                return;

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
                    if(_net.IsServer)
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

    private void OnDropAttempt(Entity<AltBlockingComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancel();
    }

    private void OnThrowAttempt(Entity<AltBlockingComponent> ent, ref ThrowItemAttemptEvent args)
    {
        if (IsDropBlocked(ent))
            args.Cancelled = false;
    }

    private void OnEquip(Entity<AltBlockingComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.User = args.User;
        Dirty(ent.Owner, ent.Comp);

        var userComp = EnsureComp<AltBlockingUserComponent>(args.User);
        userComp.BlockingItemsShields.Add(ent.Owner);

        _actionsSystem.AddAction(args.User, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.User);
        Dirty(args.User, userComp);
    }

    private void OnGotEquip(Entity<AltBlockingComponent> ent, ref GotEquippedEvent args)
    {

        if (!ent.Comp.AvaliableSlots.ContainsKey(args.SlotFlags))
            return;

        ent.Comp.User = args.Equipee;
        Dirty(ent.Owner, ent.Comp);

        var userComp = EnsureComp<AltBlockingUserComponent>(args.Equipee);
        _actionsSystem.AddAction(args.Equipee, ref userComp.BlockingToggleActionEntity, userComp.BlockingToggleAction, args.Equipee);

        userComp.BlockingItemsShields.Add(ent.Owner);
        Dirty(args.Equipee, userComp);
    }

    private void OnGotUnequipped(Entity<AltBlockingComponent> ent, ref GotUnequippedEvent args)
    {
        StopBlockingHelper(ent.Owner, ent.Comp, args.Equipee);
    }

    private void OnUnequip(Entity<AltBlockingComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopBlockingHelper(ent.Owner, ent.Comp, args.User);
    }

    private void OnDrop(Entity<AltBlockingComponent> ent, ref DroppedEvent args)
    {
        StopBlockingHelper(ent.Owner, ent.Comp, args.User);
    }

    private void OnShutdown(Entity<AltBlockingComponent> ent, ref ComponentShutdown args)
    {
        //In theory the user should not be null when this fires off
        if (ent.Comp.User != null)
            StopBlockingHelper(ent.Owner, ent.Comp, ent.Comp.User.Value);
    }

    private bool TryBlock(List<EntityUid?> items, DamageSpecifier? damage, AltBlockingUserComponent comp)
    {
        foreach (var item in items)
        {
            if ((!TryComp<AltBlockingComponent>(item, out var blockComp)) || damage == null)
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

            if (comp.IsBlocking)
            {
                if (rand.Prob(blockComp.ActiveRangeBlockProb))
                {
                    _damageable.TryChangeDamage((EntityUid)item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                    return true;
                }
            }

            else
            {
                if (rand.Prob(blockComp.RangeBlockProb))
                {
                    _damageable.TryChangeDamage((EntityUid)item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, (EntityUid)item);
                    return true;
                }
            }
        }
        return false;
    }
}
