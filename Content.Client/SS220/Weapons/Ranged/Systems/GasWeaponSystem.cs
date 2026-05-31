// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Client.SS220.Weapons.Ranged.Systems;

public sealed class GasWeaponSystem : SharedGasWeaponSystem
{

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnShootAttempt(Entity<GasWeaponComponent>ent, ref ShotAttemptedEvent args)
    {
        base.OnShootAttempt(ent, ref args);
    }

}
