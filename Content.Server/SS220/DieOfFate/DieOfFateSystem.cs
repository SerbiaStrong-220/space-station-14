using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Dice;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
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

    public override void Initialize()
    {
        SubscribeLocalEvent<DieOfFateComponent, UseInHandEvent>(OnUseInHand, after: [typeof(DiceSystem)]);
    }

    private void OnUseInHand(Entity<DieOfFateComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<DiceComponent>(ent.Owner, out var diceComponent))
            return;

        DoAction(args.User, diceComponent.CurrentValue);
    }

    private void DoAction(EntityUid uid, int value)
    {
        switch (value)
        {
            case 1:
                OnRollOne(uid);
                break;
            case 2:
                OnRollTwo(uid);
                break;
            case 3:
                OnRollThree(uid);
                break;
            case 4:
                OnRollFour(uid);
                break;
            case 5:
                OnRollFive(uid);
                break;
            case 6:
                OnRollSix(uid);
                break;
            case 7:
                OnRollSeven(uid);
                break;
            case 8:
                OnRollEight(uid);
                break;
            case 9:
                OnRollNine(uid);
                break;
            case 10:
                OnRollTen(uid);
                break;
            case 11:
                OnRollEleven(uid);
                break;
            case 12:
                OnRollTwelve(uid);
                break;
            case 13:
                OnRollThirteen(uid);
                break;
            case 14:
                OnRollFourteen(uid);
                break;
            case 15:
                OnRollFifteen(uid);
                break;
            case 16:
                OnRollSixteen(uid);
                break;
            case 17:
                OnRollSeventeen(uid);
                break;
            case 18:
                OnRollEighteen(uid);
                break;
            case 19:
                OnRollNineteen(uid);
                break;
            case 20:
                for (var i = 0; i < 3; i++)
                {
                    var extraRoll = _random.Next(10, 20);
                    DoAction(uid, extraRoll);
                }
                break;
        }
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
        blockDamage.BlockAllTypesDamage = true;
        blockDamage.DamageCoefficient = 0.5f;
    }
}
