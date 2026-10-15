// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.SS220.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem : EntitySystem
{
    //SS220 weapon overhaul begin
    private void OnGunUsed(GunCycleRequestEvent args)
    {
        if (args.Handled)
            return;

        var localGun = GetEntity(args.Gun);

        var localUser = GetEntity(args.User);

        if (TryComp<ChamberMagazineAmmoProviderComponent>(localGun, out var chamberMagComp))
        {
            args.Handled = true;
            if (chamberMagComp.CanRack)
                UseChambered(localGun, chamberMagComp, localUser);
            else
                ToggleBolt(localGun, chamberMagComp, localUser);

            return;
        }

        if (TryComp<BallisticAmmoProviderComponent>(localGun, out var ballisticComp))
        {
            ManualCycle((localGun, ballisticComp), TransformSystem.GetMapCoordinates(localGun), localUser);
            args.Handled = true;

            return;
        }

        if(TryComp<RevolverAmmoProviderComponent>(localGun, out var revolverComp))
        {
            if (args.Handled)
                return;

            if (!_useDelay.TryResetDelay(localGun))
                return;

            args.Handled = true;

            Cycle(revolverComp);
            UpdateAmmoCount(localGun, prediction: false);
            Dirty(localGun, revolverComp);
        }
    }
    //SS220 weapon overhaul end
}
