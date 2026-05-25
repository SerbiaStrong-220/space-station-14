// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
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
