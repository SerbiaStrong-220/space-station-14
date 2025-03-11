using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.EntityEffects.Effects;
using Content.Server.EntityEffects.Effects.StatusEffects;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.SS220.DieOfFate;
using Content.Server.Stunnable;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Pinpointer;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.EntityBlockDamage;
using Content.Shared.SS220.RemoveEffects;
using Content.Shared.SS220.Tarot;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Traits.Assorted;
using Content.Shared.VendingMachines;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Tarot;

public sealed class TarotCardSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string TarotCardEffectPrototype = "EffectTarotCard";

    private readonly List<string> _headsOfDepartment =
    [
        "HeadOfPersonal",
        "HeadOfSecurity",
        "ChiefEngineer",
        "Captain",
    ];

    private const string MedkitCombatFilled = "MedkitCombatFilled";
    private const string SpaceCash500 = "SpaceCash500";
    private const string BlockGameArcade = "BlockGameArcade";
    private const string D20Dice = "d20Dice";
    private const string ArrivalBeaconTag = "station-beacon-arrivals";
    private const string BridgeBeaconTag = "station-beacon-bridge";

    public override void Initialize()
    {
        SubscribeLocalEvent<TarotCardComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<TarotCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TarotCardComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TarotCardComponent, ThrowDoHitEvent>(OnThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TarotCardComponent>();

        while (query.MoveNext(out var card, out var cardComponent))
        {
            if (cardComponent.EntityEffect != null && !Exists(cardComponent.EntityEffect.Value))
            {
                QueueDel(card);
            }
        }
    }

    private void OnCompInit(Entity<TarotCardComponent> ent, ref ComponentInit args)
    {
        ent.Comp.IsReversed = _random.Next(2) == 0;
        _appearance.SetData(ent.Owner, TarotVisuals.Reversed, ent.Comp.IsReversed);
    }

    private void OnExamined(Entity<TarotCardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.IsReversed ? "tarot-card-is-reverse" : "tarot-card-is-not-reverse"));
    }

    private void OnUseInHand(Entity<TarotCardComponent> ent, ref UseInHandEvent args)
    {
        if (_gameTiming.CurTime < ent.Comp.NextUpdate)
        {
            _popup.PopupEntity("Карта пока не готова!", args.User, args.User);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            return;

        ent.Comp.User = args.User;
        HandleCardEffect(ent, args.User);
    }

    private void OnThrow(Entity<TarotCardComponent> ent, ref ThrowDoHitEvent args)
    {
        if (_gameTiming.CurTime < ent.Comp.NextUpdate)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        ent.Comp.User = null;
        ent.Comp.Target = args.Target;
        HandleCardEffect(ent, args.Target);
    }

    private void HandleCardEffect(Entity<TarotCardComponent> card, EntityUid target)
    {
        if (MetaData(card).EntityPrototype == null)
            return;

        var entityEffect = SpawnAttachedTo(TarotCardEffectPrototype, Transform(target).Coordinates);
        card.Comp.EntityEffect = entityEffect;

        if (!_transform.IsParentOf(Transform(target), entityEffect))
        {
            _transform.SetParent(entityEffect, target);
        }

        switch (MetaData(card).EntityPrototype!.ID)
        {
            case "TarotFoolCard":
                ApplyReversedEffect(card, target, ClearInventory, TeleportToArrivals);
                break;
            case "TarotMagicianCard":
                ApplyReversedEffect(card, target, EnsurePacified, OpenNearestAirlock); //TODO
                break;
            case "TarotEmpressCard":
                ApplyReversedEffect(card, target, EnsurePacified, TransferSolution);
                break;
            case "TarotEmperorCard":
                ApplyReversedEffect(card, target, TeleportToHoD, TeleportToBridge);
                break;
            case "TarotLoversCard":
                ApplyReversedEffect(card, target, HurtTarget, HealTarget);
                break;
            case "TarotChariotCard":
                ApplyReversedEffect(card, target, HurtTarget, ApplyChariotEffects); //TODO
                break;
            case "TarotJusticeCard":
                ApplyReversedEffect(card, target, HurtTarget, SpawnJusticeItems); //TODO
                break;
            case "TarotHermitCard":
                ApplyReversedEffect(card, target, TransformGuns, TeleportToVend);
                break;
            case "TarotWheelOfFortuneCard":
                ApplyReversedEffect(card, target, RollDieOfFortune, CreateGambling);
                break;
            // case Strength
            // case HangedMan
            // case Death
            case "TarotTemperanceCard":
                ApplyReversedEffect(card, target, EatPills, HealLing);
                break;
        }

        card.Comp.NextUpdate = _gameTiming.CurTime + card.Comp.Delay;
        card.Comp.Target = null;
    }

    private static void ApplyReversedEffect(TarotCardComponent card, EntityUid target, Action<EntityUid> reversedAction, Action<EntityUid> normalAction)
    {
        if (card.IsReversed)
        {
            reversedAction(target);
        }
        else
        {
            normalAction(target);
        }
    }

    private void TeleportToArrivals(EntityUid target)
    {
        var query = EntityQueryEnumerator<NavMapBeaconComponent>();

        while (query.MoveNext(out var beacon, out var beaconComponent))
        {
            if (Transform(beacon).MapUid != Transform(target).MapUid || beaconComponent.DefaultText != ArrivalBeaconTag)
                continue;

            _transform.SetCoordinates(target, Transform(beacon).Coordinates);
            return;
        }
    }

    private void ClearInventory(EntityUid user)
    {
        if (!TryComp<InventoryComponent>(user, out var inventoryComponent))
            return;

        foreach (var container in inventoryComponent.Containers)
        {
            if (container.ContainedEntity != null)
            {
                _container.Remove(container.ContainedEntity.Value, container);
            }
        }
    }

    private void OpenNearestAirlock(EntityUid target)
    {
        var doorHashSet = _lookup.GetEntitiesInRange<AirlockComponent>(Transform(target).Coordinates, 2f);

        if (doorHashSet.Count == 0)
            return;

        var doorEntity = doorHashSet.First();

        _door.StartOpening(doorEntity);
    }

    private void TransferSolution(EntityUid target)
    {
        if (!TryComp<SolutionContainerManagerComponent>(target, out _) ||
            !_solution.TryGetSolution(target, "chemicals", out var targetSolution))
            return;

        _solution.TryAddReagent(targetSolution.Value, "Omnizine", 20f);
    }

    private void EnsurePacified(EntityUid target)
    {
        var entities = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 5f);

        foreach (var entity in entities)
        {
            if (entity.Owner == target)
                continue;

            EnsureComp<PacifiedComponent>(entity, out var pacifiedComponent);
            pacifiedComponent.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(40f);
        }
    }
    private void TeleportToBridge(EntityUid target)
    {
        var query = EntityQueryEnumerator<NavMapBeaconComponent>();

        while (query.MoveNext(out var beacon, out var beaconComponent))
        {
            if (beaconComponent.DefaultText != BridgeBeaconTag)
                continue;

            _transform.SetCoordinates(target, Transform(beacon).Coordinates);
            return;
        }
    }

    private void TeleportToHoD(EntityUid target)
    {
        List<EntityUid> heads = [];
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent>();

        while (query.MoveNext(out var entity, out _))
        {
            if (!_mind.TryGetMind(entity, out var mindId, out _))
                continue;

            if (!_job.MindTryGetJobId(mindId, out var jobProto))
                continue;

            if (_headsOfDepartment.Contains(jobProto!.Value.Id))
                heads.Add(entity);
        }

        if (heads.Count == 0)
            return;

        _random.Shuffle(heads);
        _transform.SetCoordinates(target, Transform(heads.First()).Coordinates);
    }

    private void HealTarget(EntityUid target)
    {
        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), -20);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Asphyxiation"), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Poison"), -40);

        _bloodstream.TryModifyBloodLevel(target, 100f);
        _damageable.TryChangeDamage(target, damageSpec, true);
    }

    private void HurtTarget(EntityUid target)
    {
        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), 40);
        _stun.TryParalyze(target, TimeSpan.FromSeconds(4f), false);
        _bloodstream.TryModifyBloodLevel(target, -120f);
        _damageable.TryChangeDamage(target, damageSpec, true);
    }
    private void ApplyChariotEffects(EntityUid target)
    {
        // Pacified in 10 sec after use
        EnsureComp<PacifiedComponent>(target, out var pacified);
        pacified.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(10f);

        // Remove all incoming stun and knocked down
        EnsureComp<RemoveStunComponent>(target, out var removeStun);
        removeStun.Time = 10f;

        // Reduce income damage by 90%
        EnsureComp<EntityBlockDamageComponent>(target, out var blockDamage);

        blockDamage.BlockAllDamage = true;
        blockDamage.BlockPercent = 0.9;
        blockDamage.Duration = 10f;

        // Adrenaline effect, not reagents
        var genericStatusEffect = new GenericStatusEffect()
        {
            Key = "Adrenaline",
            Component = "IgnoreSlowOnDamage",
            Time = 10f,
            Type = StatusEffectMetabolismType.Add,
        };

        // Increase move speed by 1.2 to target
        var moveSpeedEffect = new MovespeedModifier()
        {
            StatusLifetime = 10f,
            WalkSpeedModifier = 1.25f,
            SprintSpeedModifier = 1.25f,
        };

        genericStatusEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
        moveSpeedEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void SpawnJusticeItems(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        Spawn(MedkitCombatFilled, coords);
        Spawn(SpaceCash500, coords);
    }

    private void TeleportToVend(EntityUid target)
    {
        List<EntityUid> vendMach = [];
        var query = EntityQueryEnumerator<VendingMachineComponent>();
        var targetXform = Transform(target);

        while (query.MoveNext(out var uid, out _))
        {
            if (Transform(uid).MapUid == targetXform.MapUid)
                vendMach.Add(uid);
        }

        if (vendMach.Count == 0)
            return;

        _random.Shuffle(vendMach);
        _transform.SetCoordinates(target, Transform(vendMach.First()).Coordinates);
    }

    private void TransformGuns(EntityUid target)
    {
        var guns = _lookup.GetEntitiesInRange<GunComponent>(Transform(target).Coordinates, 3f);

        foreach (var gun in guns)
        {
            var coord = Transform(gun).Coordinates;
            Spawn(SpaceCash500, coord);
            QueueDel(gun);
        }
    }

    private void CreateGambling(EntityUid target)
    {
        Spawn(BlockGameArcade, Transform(target).Coordinates);
    }

    private void RollDieOfFortune(EntityUid target)
    {
        var entity = Spawn(D20Dice, MapCoordinates.Nullspace);
        EnsureComp<DieOfFateComponent>(entity);
        _interaction.UseInHandInteraction(target, entity, false, false, false);
        Del(entity);
    }

    private void HealLing(EntityUid target)
    {
        RemCompDeferred<BlindableComponent>(target);
        RemCompDeferred<PermanentBlindnessComponent>(target);
        RemCompDeferred<LegsParalyzedComponent>(target);

        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier();


        foreach (var (group, value) in damageableComponent.DamagePerGroup)
        {
            if (group != "Toxin" || value <= 0)
                continue;

            if (!_proto.TryIndex<DamageGroupPrototype>(group, out var damageType))
                continue;

            damageSpec += new DamageSpecifier(damageType, -value * damageType.DamageTypes.Count);
        }

        _damageable.TryChangeDamage(target, damageSpec);
    }

    private void EatPills(EntityUid target)
    {
        if (!TryComp<SolutionContainerManagerComponent>(target, out var targetSolutionContainer))
            return;

        if (!_solution.TryGetSolution((target, targetSolutionContainer), "chemicals", out var solComp, out _))
            return;

        var targetCoords = Transform(target).Coordinates;

        for (var i = 0; i < 4; i++)
        {
            var pill = Spawn("StrangePill", targetCoords);

            if (!TryComp<SolutionContainerManagerComponent>(pill, out var solution) ||
                !_solution.TryGetSolution((pill, solution), "food", out _, out var solutionReagent) ||
                solutionReagent.Volume == FixedPoint2.Zero)
            {
                QueueDel(pill);
                continue;
            }

            _solution.TryTransferSolution(solComp.Value, solutionReagent, solutionReagent.Volume);
            QueueDel(pill);
        }
    }
}
