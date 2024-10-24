using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    protected override void InitializeBattery()
    {
        base.InitializeBattery();
        // Hitscan
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
        SubscribeLocalEvent<HitscanBatteryAmmoProviderComponent, AppearanceChangeEvent>(OnHitscanAppearanceChange); //SS220 Add Multifaze gun

        // Projectile
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, AmmoCounterControlEvent>(OnControl);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, UpdateAmmoCounterEvent>(OnAmmoCountUpdate);
        SubscribeLocalEvent<ProjectileBatteryAmmoProviderComponent, AppearanceChangeEvent>(OnProjectileAppearanceChange); //SS220 Add Multifaze gun
    }

    private void OnAmmoCountUpdate(EntityUid uid, BatteryAmmoProviderComponent component, UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxesStatusControl boxes) return;

        boxes.Update(component.Shots, component.Capacity);
    }

    private void OnControl(EntityUid uid, BatteryAmmoProviderComponent component, AmmoCounterControlEvent args)
    {
        args.Control = new BoxesStatusControl();
    }

    //SS220 Add Multifaze gun begin
    private void OnHitscanAppearanceChange(Entity<HitscanBatteryAmmoProviderComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAmmoCount(ent);
    }

    private void OnProjectileAppearanceChange(Entity<ProjectileBatteryAmmoProviderComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAmmoCount(ent);
    }
    //SS220 Add Multifaze gun end
}
