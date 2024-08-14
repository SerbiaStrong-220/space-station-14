using Content.Shared.Weapons.Ranged.Components;
using static Content.Shared.Weapons.Ranged.Systems.BatteryWeaponFireModesSystem;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class BatteryWeaponFireModesSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryWeaponFireModesComponent, ChangeFireModeEvent>(OnFireModeChange);
    }

    private void OnFireModeChange(EntityUid uid, BatteryWeaponFireModesComponent component, ChangeFireModeEvent args)
    {
        var fireMode = component.FireModes[args.Index];

        if (fireMode.MagState is not null)
            _gunSystem.SetMagState(uid, fireMode.MagState);
    }
}
