using Content.Shared.Fluids.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.SS220.IgnoreWield;

public sealed class IgnoreWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IgnoreWieldOnEquipComponent, GotEquippedEvent>(OnEqupped);
        SubscribeLocalEvent<IgnoreWieldOnEquipComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<IgnoreWieldComponent, ComponentRemove>(OnCompRemove);
        SubscribeLocalEvent<IgnoreWieldComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<IgnoreWieldComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<IgnoreWieldComponent, DidUnequipHandEvent>(OnDidUnequipHand);
    }

    private void OnDidUnequipHand(Entity<IgnoreWieldComponent> ent, ref DidUnequipHandEvent args)
    {
        if (HasComp<GunWieldBonusComponent>(args.Unequipped))
            _gun.RefreshModifiers(args.Unequipped);
    }

    private void OnDidEquipHand(Entity<IgnoreWieldComponent> ent, ref DidEquipHandEvent args)
    {
        if (HasComp<GunWieldBonusComponent>(args.Equipped))
            _gun.RefreshModifiers(args.Equipped);
    }

    private void OnCompInit(Entity<IgnoreWieldComponent> ent, ref ComponentInit args)
    {
        RefreshWieldModifiers(ent);
    }

    private void OnCompRemove(Entity<IgnoreWieldComponent> ent, ref ComponentRemove args)
    {
        RefreshWieldModifiers(ent);
    }

    private void OnUnequipped(Entity<IgnoreWieldOnEquipComponent> ent, ref GotUnequippedEvent args)
    {
        RemComp<IgnoreWieldComponent>(args.EquipTarget);
    }

    private void OnEqupped(Entity<IgnoreWieldOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<IgnoreWieldComponent>(args.EquipTarget, out var comp);
        comp.IgnoreWeaponStats = ent.Comp.IgnoreWeaponStats;

        RefreshWieldModifiers(args.EquipTarget);
    }

    private void RefreshWieldModifiers(EntityUid entity)
    {
        var held = _hands.EnumerateHeld(entity);

        foreach (var item in held)
        {
            if (HasComp<GunWieldBonusComponent>(item))
                _gun.RefreshModifiers(item);
        }
    }
}
