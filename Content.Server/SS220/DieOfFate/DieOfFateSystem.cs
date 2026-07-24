using System.Linq;
using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Dice;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Database;
using Content.Shared.Dice;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.EntityBlockDamage;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
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
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const float MovementModifier = 0.8f;

    private const string HighRiskItem = "HighRiskItem";

    private const string Monkey = "Monkey";
    private const string MobAdultSlimesBlue = "MobAdultSlimesBlue";
    private const string MobGoliath = "MobGoliath";

    private const string Brute = "Brute";
    private const string Blunt = "Blunt";

    private const string CaptainIDCard = "CaptainIDCard";
    private const string WeaponRifleAk = "WeaponRifleAk";
    private const string SpaceCash20000 = "SpaceCash20000";
    private const string KnockSpellbook = "KnockSpellbook";
    private const string ClothingBackpackChameleonFill = "ClothingBackpackChameleonFill";
    private const string SpaceCash50000 = "SpaceCash50000";
    private const string FoodCakeBlueberry = "FoodCakeBlueberry";

    public override void Initialize()
    {
        SubscribeLocalEvent<DieOfFateComponent, UseInHandEvent>(OnUseInHand, after: [typeof(DiceSystem)]);
    }

    private void OnUseInHand(Entity<DieOfFateComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.IsUsed)
            return;

        if (!TryComp<DiceComponent>(ent.Owner, out var diceComponent))
            return;

        DoAction(args.User, diceComponent.CurrentValue);
        args.Handled = true;
        ent.Comp.IsUsed = true;
    }

    public void DoAction(EntityUid user, int value)
    {
        _popup.PopupEntity(Loc.GetString("tarot-cards-roll-dice", ("value", value)), user, user);

        switch (value)
        {
            case 1:
                OnRollOne(user);
                break;
            case 2:
                OnRollTwo(user);
                break;
            case 3:
                OnRollThree(user);
                break;
            case 4:
                OnRollFour(user);
                break;
            case 5:
                OnRollFive(user);
                break;
            case 6:
                OnRollSix(user);
                break;
            case 7:
                OnRollSeven(user);
                break;
            case 8:
                OnRollEight(user);
                break;
            case 9:
                OnRollNine(user);
                break;
            case 10:
                OnRollTen(user);
                break;
            case 11:
                OnRollEleven(user);
                break;
            case 12:
                OnRollTwelve(user);
                break;
            case 13:
                OnRollThirteen(user);
                break;
            case 14:
                OnRollFourteen(user);
                break;
            case 15:
                OnRollFifteen(user);
                break;
            case 16:
                OnRollSixteen(user);
                break;
            case 17:
                OnRollSeventeen(user);
                break;
            case 18:
                OnRollEighteen(user);
                break;
            case 19:
                OnRollNineteen(user);
                break;
            case 20:
                for (var i = 0; i < 3; i++)
                {
                    var extraRoll = _random.Next(10, 20);
                    DoAction(user, extraRoll);
                }
                break;
        }

        _adminLog.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(user):player} use Roll of Dice and value is {value}");
    }

    private void OnRollOne(EntityUid user)
    {
        _body.GibBody(user, true);
    }

    private void OnRollTwo(EntityUid user)
    {
        if (!TryComp<DamageableComponent>(user, out var damageableComponent))
            return;

        _suicide.ApplyLethalDamage((user, damageableComponent), Blunt);
        _popup.PopupEntity(Loc.GetString("tarot-cards-dice-death"), user);
    }

    private void OnRollThree(EntityUid user)
    {
        SpawnAtPosition(MobGoliath, Transform(user).Coordinates);
    }

    private void OnRollFour(EntityUid user)
    {
        if (!TryComp<InventoryComponent>(user, out var inventoryComponent))
            return;

        foreach (var slot in inventoryComponent.Containers)
        {
            if (slot.ContainedEntity == null)
                continue;

            if (_tag.HasTag(slot.ContainedEntity.Value, HighRiskItem))
                continue;

            if (TryComp<StorageComponent>(slot.ContainedEntity.Value, out var storage))
            {
                foreach (var item in storage.Container.ContainedEntities.ToList())
                {
                    if (_tag.HasTag(item, HighRiskItem))
                        _container.TryRemoveFromContainer(item, true);
                }
            }

            QueueDel(slot.ContainedEntity);
        }
    }

    private void OnRollFive(EntityUid user)
    {
        _polymorph.PolymorphEntity(user, MobAdultSlimesBlue);
    }

    private void OnRollSix(EntityUid user)
    {
        if (!TryComp<MovementSpeedModifierComponent>(user, out var modifierComponent))
            return;

        _movement.ChangeBaseSpeed(user, modifierComponent.BaseWalkSpeed * MovementModifier, modifierComponent.BaseSprintSpeed * MovementModifier, modifierComponent.Acceleration);
    }

    private void OnRollSeven(EntityUid user)
    {
        _throwing.TryThrow(user, Transform(user).Coordinates, 40f);
        _stun.TryAddParalyzeDuration(user, TimeSpan.FromSeconds(12));

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>(Brute), 50f);

        _damageable.TryChangeDamage(user, damageSpec, true);
    }

    private void OnRollEight(EntityUid user)
    {
        _explosion.QueueExplosion(user, "Default", 40, 1, 0, canCreateVacuum: false);
    }

    private void OnRollNine(EntityUid user)
    {
        _polymorph.PolymorphEntity(user, Monkey);
    }

    private void OnRollTen(EntityUid _)
    {
        // DO NOTHING
    }

    private void OnRollEleven(EntityUid user)
    {
        SpawnAtPosition(FoodCakeBlueberry, Transform(user).Coordinates);
    }

    private void OnRollTwelve(EntityUid user)
    {
        _rejuvenate.PerformRejuvenate(user);
    }

    private void OnRollThirteen(EntityUid user)
    {
        SpawnAtPosition(SpaceCash50000, Transform(user).Coordinates);
    }

    private void OnRollFourteen(EntityUid user)
    {
        SpawnAtPosition(ClothingBackpackChameleonFill, Transform(user).Coordinates);
    }

    private void OnRollFifteen(EntityUid user)
    {
        SpawnAtPosition(KnockSpellbook, Transform(user).Coordinates);
    }

    private void OnRollSixteen(EntityUid user)
    {
        SpawnAtPosition(SpaceCash20000, Transform(user).Coordinates);
    }

    private void OnRollSeventeen(EntityUid user)
    {
        SpawnAtPosition(WeaponRifleAk, Transform(user).Coordinates);
    }

    private void OnRollEighteen(EntityUid user)
    {
        SpawnAtPosition(CaptainIDCard, Transform(user).Coordinates);
    }

    private void OnRollNineteen(EntityUid user)
    {
        EnsureComp<EntityBlockDamageComponent>(user, out var blockDamage);
        blockDamage.BlockAllTypesDamage = true;
        blockDamage.DamageCoefficient = 0.5f;
    }
}
