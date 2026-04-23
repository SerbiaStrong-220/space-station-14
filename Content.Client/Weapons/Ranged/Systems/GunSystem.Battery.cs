using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();

        SubscribeLocalEvent<BatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<BatteryAmmoProviderComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAmmoCountUpdate(Entity<BatteryAmmoProviderComponent> ent, ref UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes)
            return;

        boxes.Update(ent.Comp.Shots, ent.Comp.Capacity);
    }

    private void OnControl(Entity<BatteryAmmoProviderComponent> ent, ref AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }

    //SS220 Add Multifaze gun begin
    // TODO UPSTREAM FIX ME
    private void OnAppearanceChange(Entity<BatteryAmmoProviderComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAmmoCount(ent);
    }
    //SS220 Add Multifaze gun end
}
