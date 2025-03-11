using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Armor;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Dice;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.EntityBlockDamage;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.DieOfFate;

public sealed class DieOfFateSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public DieOfFateSystem()
    {
        _diceActions = new Dictionary<int, Action<EntityUid>>
        {
            { 1, OnRollOne },
            { 2, OnRollTwo },
            { 3, OnRollThree },
            { 4, OnRollFour },
            { 5, OnRollFive },
            { 6, OnRollSix },
            { 7, OnRollSeven },
            { 8, OnRollEight },
            { 9, OnRollNine },
            { 10, OnRollTen },
            { 11, OnRollEleven },
            { 12, OnRollTwelve },
            { 13, OnRollThirteen },
            { 14, OnRollFourteen },
            { 15, OnRollFifteen },
            { 16, OnRollSixteen },
            { 17, OnRollSeventeen },
            { 18, OnRollEighteen },
            { 19, OnRollNineteen },
        };
    }

    private readonly Dictionary<int, Action<EntityUid>> _diceActions;

    public override void Initialize()
    {
        SubscribeLocalEvent<DieOfFateComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<DieOfFateComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<DiceComponent>(ent.Owner, out var diceComponent))
            return;

        if (diceComponent.CurrentValue == 20)
        {
            for (var i = 0; i < 3; i++)
            {
                var extraRoll = _random.Next(10, 20);

                if (_diceActions.TryGetValue(extraRoll, out var actionExtra))
                    actionExtra.Invoke(ent.Owner);
            }
            return;
        }

        if (_diceActions.TryGetValue(diceComponent.CurrentValue, out var action))
            action.Invoke(args.User);
    }

    private void OnRollOne(EntityUid target)
    {
        _body.GibBody(target, true);
        _popup.PopupEntity("Вас превратило в пепел", target);
    }

    private void OnRollTwo(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        _suicide.ApplyLethalDamage((target, damageableComponent), "Blunt");
        _popup.PopupEntity("Вас убило!", target);
    }

    private void OnRollThree(EntityUid target)
    {
        SpawnAtPosition("MobGoliath", Transform(target).Coordinates);
    }

    private void OnRollFour(EntityUid target)
    {
        if(!TryComp<InventoryComponent>(target, out var inventoryComponent))
            return;

        foreach (var slot in inventoryComponent.Containers)
        {
            if(slot.ContainedEntity != null)
                QueueDel(slot.ContainedEntity);
        }
    }

    private void OnRollFive(EntityUid target)
    {
        _polymorph.PolymorphEntity(target, "Monkey");
    }

    private void OnRollSix(EntityUid target)
    {
        if (!TryComp<MovementSpeedModifierComponent>(target, out var modifierComponent))
            return;

        _movement.ChangeBaseSpeed(target, modifierComponent.BaseWalkSpeed - 0.2f, modifierComponent.BaseSprintSpeed - 0.2f, modifierComponent.Acceleration);
    }

    private void OnRollSeven(EntityUid target)
    {
        _throwing.TryThrow(target, Transform(target).Coordinates, 40f);
        _stun.TryParalyze(target, TimeSpan.FromSeconds(12), false);

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 50f);

        _damageable.TryChangeDamage(target, damageSpec, true);
    }

    private void OnRollEight(EntityUid target)
    {
        _explosion.QueueExplosion(target, "Default", 40, 1, 0);
    }

    private void OnRollNine(EntityUid target)
    {
        _polymorph.PolymorphEntity(target, "Monkey");
    }

    private void OnRollTen(EntityUid target)
    {
        // DO NOTHING
    }

    private void OnRollEleven(EntityUid target)
    {
        SpawnAtPosition("FoodCakeBlueberry", Transform(target).Coordinates);
    }

    private void OnRollTwelve(EntityUid target)
    {
        _rejuvenate.PerformRejuvenate(target);
    }

    private void OnRollThirteen(EntityUid target)
    {
        SpawnAtPosition("SpaceCash50000", Transform(target).Coordinates);
    }

    private void OnRollFourteen(EntityUid target)
    {
        SpawnAtPosition("ClothingBackpackChameleonFill", Transform(target).Coordinates);
    }

    private void OnRollFifteen(EntityUid target)
    {
        SpawnAtPosition("KnockSpellbook", Transform(target).Coordinates);
    }

    private void OnRollSixteen(EntityUid target)
    {
        SpawnAtPosition("SpaceCash20000", Transform(target).Coordinates);
    }

    private void OnRollSeventeen(EntityUid target)
    {
        SpawnAtPosition("WeaponRifleAk", Transform(target).Coordinates);
    }

    private void OnRollEighteen(EntityUid target)
    {
        SpawnAtPosition("CaptainIDCard", Transform(target).Coordinates);
    }

    private void OnRollNineteen(EntityUid target)
    {
        EnsureComp<EntityBlockDamageComponent>(target, out var blockDamage);
        blockDamage.BlockAllDamage = true;
        blockDamage.BlockPercent = 0.5;

    }
}
