// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    //Basically rewrittem loading for BallisticAmmoProvider
    private void OnInteractUsing(Entity<ChamberMagazineAmmoProviderComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.CanBeLoadedManually ||
            args.Handled ||
            args.Used == args.Target ||
            Deleted(args.Target))
            return;

        if (args.Used is not { Valid: true } usedEntValidated)
            return;

        var magEnt = GetMagazineEntity(ent.Owner);

        if (magEnt is not { Valid: true } magEntValidated)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(magEntValidated, out var magComp) || magComp.Entities.Count >= magComp.Capacity)
            return;

        if (!_whitelistSystem.IsWhitelistFail(magComp.Whitelist, usedEntValidated))
        {
            if (ent.Comp.MustBeLoadedThroughChamber)
            {
                InsertCartridgeThroughChamber(usedEntValidated, (magEntValidated, magComp), ent);

                Audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);
                UpdateAmmoCount(ent);
                args.Handled = true;

                return;
            }
            InsertCartridge(usedEntValidated, (magEntValidated, magComp), ent);

            Audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);
            UpdateAmmoCount(ent);
            args.Handled = true;

            return;
        }

        if (!TryComp<BallisticAmmoProviderComponent>(usedEntValidated, out var usedComponent) ||
            !usedComponent.MayTransfer ||
            usedComponent.Whitelist == null)
            return;

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, usedComponent.FillDelay, new AmmoFillDoAfterEvent(), used: usedEntValidated, target: args.Target, eventTarget: args.Target)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = true,
        });
    }

    private void OnChamberMagAmmoFillDoAfter(Entity<ChamberMagazineAmmoProviderComponent> ent, ref AmmoFillDoAfterEvent args)
    {
        if (args.Used is not { Valid: true } usedEntValidated)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(usedEntValidated, out var usedComp) || (usedComp.Entities.Count <= 0 && usedComp.UnspawnedCount <= 0))
            return;

        if (!ent.Comp.CanBeLoadedManually)
            return;

        if (ent.Comp.BoltClosed == null || (bool)ent.Comp.BoltClosed && ent.Comp.MustBeLoadedThroughChamber)
            return;

        var magEnt = GetMagazineEntity(ent.Owner);

        if (magEnt is not { Valid: true } magEntValidated)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(magEntValidated, out var magComp) || magComp.Entities.Count >= magComp.Capacity)
            return;

        bool repeat = false;

        if (ent.Comp.MustBeLoadedThroughChamber)
        {
            if (usedComp.Entities.Count > 0)
            {
                if (!CanInsertBallistic((magEntValidated, magComp), usedComp.Entities[^1]))
                    return;

                InsertCartridgeThroughChamber(usedComp.Entities[^1], (magEntValidated, magComp), ent);
                usedComp.Entities.Remove(usedComp.Entities[^1]);
                Dirty(usedEntValidated, usedComp);

                repeat = magComp.Entities.Count + magComp.UnspawnedCount < magComp.Capacity && usedComp.Entities.Count + usedComp.UnspawnedCount > 0;

                args.Repeat = repeat;

                Audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);

                UpdateBallisticAppearance((usedEntValidated, usedComp));

                return;
            }

            usedComp.UnspawnedCount--;
            var newAmmoEntityChamber = Spawn(usedComp.Proto, TransformSystem.GetMapCoordinates(ent));

            if (!CanInsertBallistic((magEntValidated, magComp), newAmmoEntityChamber))
            {
                usedComp.Entities.Add(newAmmoEntityChamber);
                Containers.Insert(newAmmoEntityChamber, usedComp.Container);

                Dirty(usedEntValidated, usedComp);

                if (IsClientSide(newAmmoEntityChamber))
                    Del(newAmmoEntityChamber);

                return;
            }

            DirtyField<BallisticAmmoProviderComponent>((usedEntValidated, usedComp), nameof(BallisticAmmoProviderComponent.UnspawnedCount));

            InsertCartridgeThroughChamber(newAmmoEntityChamber, (magEntValidated, magComp), ent);

            repeat = magComp.Entities.Count + magComp.UnspawnedCount < magComp.Capacity && usedComp.Entities.Count + usedComp.UnspawnedCount > 0;

            args.Repeat = repeat;

            Audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);

            UpdateBallisticAppearance((usedEntValidated, usedComp));

            return;
        }

        if (usedComp.Entities.Count > 0)
        {
            if (!CanInsertBallistic((magEntValidated, magComp), usedComp.Entities[^1]))
                return;

            InsertCartridge(usedComp.Entities[^1], (magEntValidated, magComp), ent);
            usedComp.Entities.Remove(usedComp.Entities[^1]);
            Dirty(usedEntValidated, usedComp);

            repeat = magComp.Entities.Count + magComp.UnspawnedCount < magComp.Capacity && usedComp.Entities.Count + usedComp.UnspawnedCount > 0;

            args.Repeat = repeat;

            Audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);

            UpdateBallisticAppearance((usedEntValidated, usedComp));

            return;
        }

        usedComp.UnspawnedCount--;
        var newAmmoEntity = Spawn(usedComp.Proto, TransformSystem.GetMapCoordinates(ent));

        if (!CanInsertBallistic((magEntValidated, magComp), newAmmoEntity))
        {
            usedComp.Entities.Add(newAmmoEntity);
            Containers.Insert(newAmmoEntity, usedComp.Container);

            Dirty(usedEntValidated, usedComp);

            if (IsClientSide(newAmmoEntity))
                Del(newAmmoEntity);

            return;
        }

        DirtyField<BallisticAmmoProviderComponent>((usedEntValidated, usedComp), nameof(BallisticAmmoProviderComponent.UnspawnedCount));

        InsertCartridge(newAmmoEntity, (magEntValidated, magComp), ent);

        repeat = magComp.Entities.Count + magComp.UnspawnedCount < magComp.Capacity && usedComp.Entities.Count + usedComp.UnspawnedCount > 0;

        args.Repeat = repeat;

        UpdateBallisticAppearance((usedEntValidated, usedComp));

        Audio.PlayPredicted(usedComp.SoundInsert, ent, args.User);
    }

    private void InsertCartridgeThroughChamber(EntityUid cartridge, Entity<BallisticAmmoProviderComponent> mag, Entity<ChamberMagazineAmmoProviderComponent> gun)
    {
        var chamberEnt = GetChamberEntity(gun);

        if (chamberEnt != null && chamberEnt is { Valid: true } chamberEntValidated)
        {
            mag.Comp.Entities.Add(chamberEntValidated);
            Containers.Insert(chamberEntValidated, mag.Comp.Container);
        }

        TryInsertChamber(gun, cartridge);

        Dirty(gun);
        Dirty(mag);

        UpdateAmmoCount(gun);
    }

    private void InsertCartridge(EntityUid cartridge, Entity<BallisticAmmoProviderComponent> mag, Entity<ChamberMagazineAmmoProviderComponent> gun)
    {
        mag.Comp.Entities.Add(cartridge);
        Containers.Insert(cartridge, mag.Comp.Container);

        Dirty(gun);
        Dirty(mag);

        UpdateAmmoCount(gun);
    }
}
