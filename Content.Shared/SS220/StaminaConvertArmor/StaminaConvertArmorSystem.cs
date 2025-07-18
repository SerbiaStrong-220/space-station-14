using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.StaminaConvertArmor;

/// <summary>
/// Handles the logic for <see cref="StaminaConvertArmorComponent"/>:
/// - Converts a portion of stamina damage into electrical-type damage.
/// - Blocks specified status effects from applying while the armor is equipped.
/// - Relays events via inventory slots to ensure effects apply correctly when worn.
/// </summary>
public sealed class StaminaConvertArmorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // ref events
        SubscribeLocalEvent<StaminaConvertArmorComponent, BeforeStaminaDamageEvent>(OnStaminaResist);
        SubscribeLocalEvent<StaminaConvertArmorComponent, BeforeStatusEffectAddedRelayEvent>(OnBeforeAddStatus);

        // ref relay events
        SubscribeLocalEvent<StaminaConvertArmorComponent, InventoryRelayedEvent<BeforeStaminaDamageEvent>>(RelayedResistance);
        SubscribeLocalEvent<StaminaConvertArmorComponent, InventoryRelayedEvent<BeforeStatusEffectAddedRelayEvent>>(RelayedEffects);

        // clothing events
        SubscribeLocalEvent<StaminaConvertArmorComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<StaminaConvertArmorComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    /// <summary>
    /// Converts a portion of stamina damage into another damage type when applicable.
    /// </summary>
    private void OnStaminaResist(Entity<StaminaConvertArmorComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (ent.Comp.User == null)
            return;

        var user = GetEntity(ent.Comp.User.Value);
        if (_mobState.IsDead(user))
            return;

        if (!_proto.TryIndex(ent.Comp.DamageType, out var damageProto))
            return;

        if (args.Value <= 0)
            return;

        var toConvert = args.Value * ent.Comp.DamageCoefficient;

        _damage.TryChangeDamage(user, new DamageSpecifier(damageProto, FixedPoint2.New(toConvert)));
        args.Value -= toConvert;
    }

    /// <summary>
    /// Prevents the application of blocked status effects while armor is worn.
    /// </summary>
    private void OnBeforeAddStatus(Entity<StaminaConvertArmorComponent> ent, ref BeforeStatusEffectAddedRelayEvent args)
    {
        if (ent.Comp.User == null)
            return;

        if (ent.Comp.IgnoredEffects.Contains(args.Key))
            args.Cancelled = true;
    }

    /// <summary>
    /// Forwards stamina resistance logic via an inventory relay system.
    /// </summary>
    private void RelayedResistance(Entity<StaminaConvertArmorComponent> ent, ref InventoryRelayedEvent<BeforeStaminaDamageEvent> args)
    {
        OnStaminaResist(ent, ref args.Args);
    }

    /// <summary>
    /// Forwards status effect filtering logic via an inventory relay system.
    /// </summary>
    private void RelayedEffects(Entity<StaminaConvertArmorComponent> ent, ref InventoryRelayedEvent<BeforeStatusEffectAddedRelayEvent> args)
    {
        OnBeforeAddStatus(ent, ref args.Args);
    }

    /// <summary>
    /// Stores the entity that equipped the armor.
    /// </summary>
    private void OnGotEquipped(Entity<StaminaConvertArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.User = GetNetEntity(args.Wearer);
        Dirty(ent);
    }

    /// <summary>
    /// Clears the wearer reference when armor is unequipped.
    /// </summary>
    private void OnGotUnequipped(Entity<StaminaConvertArmorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.User = null;
        Dirty(ent);
    }
}
