// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Armor;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.SS220.AltArmor;
using Content.Shared.SS220.AltArmor.Components;

namespace Content.Shared.SS220.WearableAltArmor;

public sealed class WearableAltArmorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AltArmorSystem _altArmor = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WearableAltArmorComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    public void OnDamageModify(Entity<WearableAltArmorComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        _altArmor.ModifyDamage(ent.Owner, args.Args.OriginalDamage, out var resultDamage, out var resultArmorDamage);

        args.Args.Damage = resultDamage;

        _damageable.TryChangeDamage(ent.Owner, resultArmorDamage);
    }
}
