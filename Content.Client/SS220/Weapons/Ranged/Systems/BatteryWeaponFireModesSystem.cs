// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Client.SS220.Weapons.Ranged.Systems;

public sealed partial class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ChangeFireModeEvent>(OnFireModeChange);
    }

    private void OnFireModeChange(Entity<BatteryWeaponFireModesComponent> ent, ref ChangeFireModeEvent args)
    {
        var fireMode = ent.Comp.FireModes[args.Index];

        if (fireMode.MagState is not null)
            _gunSystem.SetMagState(ent, fireMode.MagState);
    }
}
