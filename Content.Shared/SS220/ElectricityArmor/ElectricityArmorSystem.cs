using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ElectricityArmor;

/// <summary>
/// Handles the logic for <see cref="ElectricityArmorComponent"/>:
/// - Converts a portion of stamina damage into electrical-type damage.
/// - Blocks specified status effects from applying while the armor is equipped.
/// - Relays events via inventory slots to ensure effects apply correctly when worn.
/// </summary>
public sealed class ElectricityArmorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // ref events
        SubscribeLocalEvent<ElectricityArmorComponent, BeforeStaminaDamageEvent>(OnStaminaResist);
        SubscribeLocalEvent<ElectricityArmorComponent, BeforeStatusEffectAddAttemptEvent>(OnBeforeAddStatus);

        // ref relay events
        SubscribeLocalEvent<ElectricityArmorComponent, InventoryRelayedEvent<BeforeStaminaDamageEvent>>(RelayedResistance);
        SubscribeLocalEvent<ElectricityArmorComponent, InventoryRelayedEvent<BeforeStatusEffectAddAttemptEvent>>(RelayedEffects);

        // clothing events
        SubscribeLocalEvent<ElectricityArmorComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ElectricityArmorComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    /// <summary>
    /// Converts a portion of stamina damage into another damage type when applicable.
    /// </summary>
    private void OnStaminaResist(Entity<ElectricityArmorComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.User == null)
            return;

        if (_mobState.IsDead(ent.Comp.User.Value))
            return;

        if (!_proto.TryIndex(ent.Comp.DamageType, out var damageProto))
            return;

        if (args.Value <= 0)
            return;

        var toConvert = args.Value * ent.Comp.DamageCoefficient;

        _damage.TryChangeDamage(ent.Comp.User, new DamageSpecifier(damageProto, FixedPoint2.New(toConvert)));
        args.Value -= toConvert;
    }

    /// <summary>
    /// Prevents the application of blocked status effects while armor is worn.
    /// </summary>
    private void OnBeforeAddStatus(Entity<ElectricityArmorComponent> ent, ref BeforeStatusEffectAddAttemptEvent args)
    {
        if (ent.Comp.User == null)
            return;

        if (ent.Comp.IgnoredEffects.Contains(args.Key))
            args.Cancelled = true;
    }

    /// <summary>
    /// Forwards stamina resistance logic via an inventory relay system.
    /// </summary>
    private void RelayedResistance(Entity<ElectricityArmorComponent> ent, ref InventoryRelayedEvent<BeforeStaminaDamageEvent> args)
    {
        OnStaminaResist(ent, ref args.Args);
    }

    /// <summary>
    /// Forwards status effect filtering logic via an inventory relay system.
    /// </summary>
    private void RelayedEffects(Entity<ElectricityArmorComponent> ent, ref InventoryRelayedEvent<BeforeStatusEffectAddAttemptEvent> args)
    {
        OnBeforeAddStatus(ent, ref args.Args);
    }

    /// <summary>
    /// Stores the entity that equipped the armor.
    /// </summary>
    private void OnGotEquipped(Entity<ElectricityArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = args.Wearer;
    }

    /// <summary>
    /// Clears the wearer reference when armor is unequipped.
    /// </summary>
    private void OnGotUnequipped(Entity<ElectricityArmorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
    }
}
