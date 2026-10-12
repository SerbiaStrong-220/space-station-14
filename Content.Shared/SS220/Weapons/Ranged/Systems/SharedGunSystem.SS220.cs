// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

using Content.Shared.SS220.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem : EntitySystem
{
    //SS220 weapon overhaul begin
    private void OnGunUsed(Entity<GunComponent> gun, ref GunCycleRequestEvent args)
    {
        if (args.Handled)
            return;

        if (args.User is not { Valid: true } user)
            return;

        if (TryComp<ChamberMagazineAmmoProviderComponent>(gun, out var chamberMagComp))
        {
            args.Handled = true;
            if (chamberMagComp.CanRack)
                UseChambered(gun, chamberMagComp, user);
            else
                ToggleBolt(gun, chamberMagComp, user);

            return;
        }

        if (TryComp<BallisticAmmoProviderComponent>(gun, out var ballisticComp))
        {
            ManualCycle((gun, ballisticComp), TransformSystem.GetMapCoordinates(gun), user);
            args.Handled = true;

            return;
        }
    }
    //SS220 weapon overhaul end
}
