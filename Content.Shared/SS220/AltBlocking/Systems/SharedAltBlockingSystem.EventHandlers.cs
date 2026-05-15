// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.ToggleBlocking;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
//using Content.Shared.Weapons.Hitscan.Components;
//using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.Commands.Math;
using System.Numerics;
using System.Security.Cryptography;

namespace Content.Shared.SS220.AltBlocking;

public sealed partial class SharedAltBlockingSystem
{
    private void OnBlockUserCollide(Entity<AltBlockingUserComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        var projectileAngle = _transform.GetWorldRotation(args.ProjUid);
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, projectileAngle + new Angle(Math.PI).Reduced());
    }

    private void OnBlockThrownProjectile(Entity<AltBlockingUserComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        var itemPos = _transform.GetWorldPosition(args.DamageDealer);
        var targetPos = _transform.GetWorldPosition(ent);
        var angle = new Angle(new Vector2(targetPos.X - itemPos.X, targetPos.Y - itemPos.Y)) - new Angle(Math.PI / 2);
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, angle);
    }

    private void OnBlockUserHitscan(Entity<AltBlockingUserComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        var vector = _transform.GetWorldPosition(ent) - _transform.GetWorldPosition(args.Shooter);
        args.CancelledHit = TryBlock(ent.Comp.BlockingItemsShields, args.Damage, ent, vector.ToAngle() - new Angle(Math.PI / 2));
    }

    private void OnBlockUserMeleeHit(Entity<AltBlockingUserComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        var targetPos = _transform.GetWorldPosition(ent);
        var attackerPos = _transform.GetWorldPosition(args.Attacker);

        Angle HitAngle = new Angle(new Vector2(targetPos.X - attackerPos.X, targetPos.Y - attackerPos.Y)) - new Angle(Math.PI / 2);

        HitAngle = HitAngle.Reduced();

        foreach (var item in ent.Comp.BlockingItemsShields)
        {
            if (!TryComp<AltBlockingComponent>(item, out var blockComp))
                continue;

            if (!IsCovered(HitAngle, blockComp.CoveredZones, _transform.GetWorldRotation(ent.Owner)))
                continue;

            if (TryComp<ToggleBlockingChanceComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                    continue;
            }

            if (!TryGetNetEntity(item, out var netItem))
                continue;

            if (netItem is not { Valid: true } netItemUid)
                continue;

            if (ent.Comp.Blocking)
            {
                if (SharedRandomExtensions.PredictedProb(_gameTiming, blockComp.ActiveMeleeBlockProb, netItemUid))
                {
                    if (_net.IsServer)
                    {
                        _audio.PlayPvs(blockComp.BlockSound, item);
                        _popupSystem.PopupEntity(Loc.GetString("block-shot"), ent.Owner);
                    }

                    args.CancelledHit = true;
                    args.Blocker = item;
                    return;
                }
            }

            else
            {
                if (SharedRandomExtensions.PredictedProb(_gameTiming, blockComp.MeleeBlockProb, netItemUid))
                {
                    if (_net.IsServer)
                    {
                        _audio.PlayPvs(blockComp.BlockSound, item);
                        _popupSystem.PopupEntity(Loc.GetString("block-shot"), ent.Owner);
                    }

                    args.CancelledHit = true;
                    args.Blocker = item;
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

    private bool TryBlock(List<EntityUid> items, DamageSpecifier? damage, Entity<AltBlockingUserComponent> owner, Angle HitRotation)
    {
        foreach (var item in items)
        {
            if (!TryComp<AltBlockingComponent>(item, out var blockComp) || damage == null)
                continue;

            if (!IsCovered(HitRotation, blockComp.CoveredZones, _transform.GetWorldRotation(owner.Owner)))
                continue;

            if (TryComp<ToggleBlockingChanceComponent>(item, out var toggleComp))
            {
                if (!toggleComp.IsToggled)
                    continue;
            }

            if (blockComp.User == null)
                return false;

            var user = (EntityUid)blockComp.User;

            if (!TryGetNetEntity(item, out var netItem))
                continue;

            if (owner.Comp.Blocking)
            {
                if (SharedRandomExtensions.PredictedProb(_gameTiming, blockComp.ActiveRangeBlockProb, (NetEntity)netItem))
                {
                    _damageable.TryChangeDamage(item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, item);
                    _popupSystem.PopupEntity(Loc.GetString("block-shot"), user);
                    return true;
                }
            }

            else
            {
                if (SharedRandomExtensions.PredictedProb(_gameTiming, blockComp.RangeBlockProb, (NetEntity)netItem))
                {
                    _damageable.TryChangeDamage(item, damage);
                    _audio.PlayPvs(blockComp.BlockSound, item);
                    _popupSystem.PopupEntity(Loc.GetString("block-shot"), user);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsCovered(Angle Incoming, int CoveredZones, Angle UserRotation)
    {
        var diff = Math.Abs((int)Incoming.GetDir() - (int)UserRotation.GetDir());
        if (diff > 4)
            diff = 8 - diff;

        return diff <= CoveredZones;
    }
}
