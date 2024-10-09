using Content.Server.SS220.DarkForces.Saint.Items.Cross;
using Content.Shared.Damage;
using Content.Shared.Inventory;

namespace Content.Server.SS220.DarkForces.Saint.Items.Cross;

public sealed partial class SaintCrossSystem
{
    private static readonly DamageModifierSet FelDamageModify = new()
    {
        Coefficients =
        {
            ["Fel"] = 0.5f
        }
    };

    private void InitializeDamage()
    {
        // Fel damage is unused in our build
        //SubscribeLocalEvent<SaintCrossComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid,
        SaintCrossComponent component,
        InventoryRelayedEvent<DamageModifyEvent> args)
    {
        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, FelDamageModify);
    }
}
