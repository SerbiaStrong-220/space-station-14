using System.Linq;
using System.Numerics;
using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.EntityEffects.Effects;
using Content.Server.EntityEffects.Effects.StatusEffects;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Implants;
using Content.Server.Popups;
using Content.Server.SS220.DieOfFate;
using Content.Server.SS220.Hallucination;
using Content.Server.Storage.Components;
using Content.Server.Stunnable;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Dataset;
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
using Content.Shared.Mobs;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Content.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.EntityBlockDamage;
using Content.Shared.SS220.EntityEffects;
using Content.Shared.SS220.HereticAbilities;
using Content.Shared.SS220.Tarot;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Traits.Assorted;
using Content.Shared.VendingMachines;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
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
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly HallucinationSystem _hallucination = default!;
    [Dependency] private readonly SubdermalImplantSystem _implantSystem = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly DieOfFateSystem _dieOfFate = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    private const string TarotCardEffectPrototype = "EffectTarotCard";

    private readonly List<string> _headsOfDepartment =
    [
        "HeadOfPersonal",
        "HeadOfSecurity",
        "ChiefEngineer",
        "Captain",
    ];

    private const string SpaceCash500 = "SpaceCash500";
    private const string MobCatCake = "MobCatCake";
    private const string MobCorgiCerberus = "MobCorgiCerberus";
    private const string ArrivalBeaconTag = "station-beacon-arrivals";
    private const string BridgeBeaconTag = "station-beacon-bridge";
    private const string Omnizine = "Omnizine";
    private const string JusticeItems = "JusticeItems";
    private const string RandomArcadeSpawner = "RandomArcadeSpawner";

    public override void Initialize()
    {
        SubscribeLocalEvent<TarotCardComponent, MapInitEvent>(OnMapInit);
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
                QueueDel(card);
        }
    }

    private void OnMapInit(Entity<TarotCardComponent> ent, ref MapInitEvent args)
    {
        if (!EntityManager.TryGetComponent(ent.Owner, out TarotCardComponent? tarot))
            return;

        tarot.IsReversed = _random.Next(2) == 0;
        _appearance.SetData(ent.Owner, TarotVisuals.Reversed, tarot.IsReversed);
    }

    private void OnExamined(Entity<TarotCardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.IsReversed ? "tarot-card-is-reverse" : "tarot-card-is-not-reverse"));
    }

    private void OnUseInHand(Entity<TarotCardComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.IsUsed)
        {
            _popup.PopupEntity(Loc.GetString("tarot-cards-failed-already-used"), args.User, args.User);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            return;

        HandleCardEffect(ent, args.User);
        args.Handled = true;
    }

    private void OnThrow(Entity<TarotCardComponent> ent, ref ThrowDoHitEvent args)
    {
        if (ent.Comp.IsUsed)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        HandleCardEffect(ent, args.Target);
        args.Handled = true;
    }

    private void HandleCardEffect(Entity<TarotCardComponent> card, EntityUid target)
    {
        if (MetaData(card).EntityPrototype == null)
            return;

        var entityEffect = SpawnAttachedTo(TarotCardEffectPrototype, Transform(target).Coordinates);
        card.Comp.EntityEffect = entityEffect;

        if (!_transform.IsParentOf(Transform(target), entityEffect))
            _transform.SetParent(entityEffect, target);

        switch (card.Comp.CardType)
        {
            case TarotCardType.Fool:
                ApplyReversedEffect(card, target, ClearInventory, TeleportToArrivals);
                break;
            case TarotCardType.Magician:
                ApplyReversedEffect(card, target, PushPlayers, OpenNearestAirlock);
                break;
            case TarotCardType.HighPriestess:
                break;
            case TarotCardType.Empress:
                ApplyReversedEffect(card, target, EnsurePacified, TransferSolution);
                break;
            case TarotCardType.Emperor:
                ApplyReversedEffect(card, target, TeleportToHoD, TeleportToBridge);
                break;
            case TarotCardType.Hierophant:
                ApplyReversedEffect(card, target, SpawnCerberus, SpawnCatCake);
                break;
            case TarotCardType.Lovers:
                ApplyReversedEffect(card, target, HurtTarget, HealTarget);
                break;
            case TarotCardType.Chariot:
                ApplyReversedEffect(card, target, SpeedUpEntity, ApplyChariotEffects);
                break;
            case TarotCardType.Strength:
                ApplyReversedEffect(card, target, MassHallucinations, HealTarget);
                break;
            case TarotCardType.Hermit:
                ApplyReversedEffect(card, target, TransformGuns, TeleportToVend);
                break;
            case TarotCardType.WheelOfFortune:
                ApplyReversedEffect(card, target, RollDieOfFortune, CreateGambling);
                break;
            case TarotCardType.Justice:
                ApplyReversedEffect(card, target, SpawnRandomCrate, SpawnJusticeItems);
                break;
            case TarotCardType.HangedMan:
                ApplyReversedEffect(card, target, FixturesSet, TeleportToRandomTile);
                break;
            case TarotCardType.Death:
                ApplyReversedEffect(card, target, ModifyThreshold, HurtAnother);
                break;
            case TarotCardType.Temperance:
                ApplyReversedEffect(card, target, EatPills, HealLing);
                break;
            case TarotCardType.Devil:
                ApplyReversedEffect(card, target, ClusterFlashBang, EatPills);
                break;
            case TarotCardType.Tower:
                break;
            case TarotCardType.Star:
                break;
            case TarotCardType.Moon:
                break;
            case TarotCardType.Sun:
                ApplyReversedEffect(card, target, SmokeGrenade, Rejuvenate);
                break;
            case TarotCardType.Judgement:
                break;
            case TarotCardType.World:
                break;
        }

        card.Comp.IsUsed = true;
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

    private void PushPlayers(EntityUid target)
    {
        var lookup = _lookup.GetEntitiesInRange(target, 4f, LookupFlags.Dynamic | LookupFlags.Sundries);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in lookup)
        {
            if (physQuery.TryGetComponent(ent, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(ent);

            var foo = _transform.GetMapCoordinates(ent, xform: tempXform).Position - _transform.GetMapCoordinates(target, xform: Transform(target)).Position;
            _throwing.TryThrow(ent, foo*2, 4f, target, 0);
        }
    }

    private void TransferSolution(EntityUid target)
    {
        if (!TryComp<SolutionContainerManagerComponent>(target, out _) ||
            !_solution.TryGetSolution(target, "chemicals", out var targetSolution))
            return;

        _solution.TryAddReagent(targetSolution.Value, Omnizine, 20f);
    }

    private void EnsurePacified(EntityUid target)
    {
        var entities =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 5f);

        foreach (var entity in entities.Where(entity => entity.Owner != target))
        {
            _statusEffects.TryAddStatusEffect(entity, "Pacified", TimeSpan.FromSeconds(40f), true);
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

    private void SpawnCatCake(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        var catCake = SpawnAtPosition(MobCatCake, coords);
        var solution = new Solution(Omnizine, FixedPoint2.New(50f));

        if (!TryComp<SolutionContainerManagerComponent>(catCake, out var containerManagerComponent))
            return;

        if (!_solution.TryGetSolution((catCake, containerManagerComponent), "food", out var solutionEnt))
            return;

        _solution.SetCapacity(solutionEnt.Value, solutionEnt.Value.Comp.Solution.Volume + solution.Volume);

        _solution.TryTransferSolution(solutionEnt.Value, solution, solution.Volume);
    }

    private void SpawnCerberus(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        var cerberus = SpawnAtPosition(MobCorgiCerberus, coords);

        _npcFaction.MakeFriendlyEntities(cerberus, target);
    }

    private void HealTarget(EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), -20);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Asphyxiation"), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>("Poison"), -40);

        _bloodstream.TryModifyBloodLevel(target, 100f);
        _damageable.TryChangeDamage(target, damageSpec, true);

        Dirty(target, damageableComponent);
    }

    private void HurtTarget(EntityUid target)
    {
        if(!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), 40);
        _stun.TryParalyze(target, TimeSpan.FromSeconds(4f), false);
        _bloodstream.TryModifyBloodLevel(target, -120f);
        _damageable.TryChangeDamage(target, damageSpec, true);

        Dirty(target, damageableComponent);
    }

    private void MassHallucinations(EntityUid target)
    {
        // TODO THIS SHIT
    }

    private void ApplyChariotEffects(EntityUid target)
    {
        // Pacified in 10 sec after use
        _statusEffects.TryAddStatusEffect<PacifiedComponent>(target, "Pacified", TimeSpan.FromSeconds(10f), true);

        // Remove all incoming stun and knock down in 10 sec
        var removeStun = new IgnoreStunEffect
        {
            RequiredEffects = ["Stun", "KnockedDown", "SlowedDown"],
            Duration = 10f,
        };

        removeStun.Effect(new EntityEffectBaseArgs(target, EntityManager));

        // Reduce income damage by 90%
        EnsureComp<EntityBlockDamageComponent>(target, out var blockDamage);

        blockDamage.BlockAllTypesDamage = true;
        blockDamage.DamageCoefficient = 0.1f;
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

    private void SpeedUpEntity(EntityUid target)
    {
        var moveSpeedEffect = new MovespeedModifier()
        {
            StatusLifetime = 30f,
            WalkSpeedModifier = 1.5f,
            SprintSpeedModifier = 1.5f,
        };
        moveSpeedEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void ModifyThreshold(EntityUid target)
    {
        var modifyThreshold = new ModifyThresholdEffect()
        {
            Duration = 500f,
            NewThresholds = new Dictionary<FixedPoint2, MobState>
            {
                { 150, MobState.Critical },
            },
        };

        modifyThreshold.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void SpawnJusticeItems(EntityUid target)
    {
        var coords = Transform(target).Coordinates;
        var items = _proto.Index<DatasetPrototype>(JusticeItems);

        foreach (var item in items.Values)
        {
            SpawnAtPosition(item, coords);
        }
    }

    private void SpawnRandomCrate(EntityUid target)
    {
        var protos = _proto.EnumeratePrototypes<CargoProductPrototype>();

        List<string> listOfProto = [];

        foreach (var proto in protos)
        {
            if (!_proto.TryIndex(proto.Product, out var prototype))
                return;

            if (prototype.HasComponent<EntityStorageComponent>())
                listOfProto.Add(prototype.ID);
        }

        _random.Shuffle(listOfProto);

        SpawnAtPosition(listOfProto.First(), Transform(target).Coordinates);
    }

    private void TeleportToRandomTile(EntityUid target)
    {
        var randomTile = _implantSystem.SelectRandomTileInRange(Transform(target), 60f);
        if (randomTile == null)
            return;

        _transform.SetCoordinates(target, randomTile.Value);
    }

    private void FixturesSet(EntityUid target)
    {
        EnsureComp<WalkThroughWallsComponent>(target, out var walkThroughWallsComponent);

        walkThroughWallsComponent.Duration = 10f;
        Dirty(target, walkThroughWallsComponent);
    }

    private void HurtAnother(EntityUid target)
    {
        var lookup =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 4f);
        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), 20);
        damageSpec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 20);

        foreach (var entity in lookup)
        {
            _damageable.TryChangeDamage(entity, damageSpec, true);
        }
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
        var arcadeList = _proto.Index<DatasetPrototype>(RandomArcadeSpawner).Values;

        var randomArcade = _random.Pick(arcadeList);
        Spawn(randomArcade, Transform(target).Coordinates);
    }

    private void RollDieOfFortune(EntityUid target)
    {
        var randomValue = _random.Next(1, 21);

        _dieOfFate.DoAction(target, randomValue);
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
        Dirty(target, damageableComponent);
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

    private void ClusterFlashBang(EntityUid target)
    {
        var cluster = SpawnAtPosition("ClusterBangFull", Transform(target).Coordinates);
        _trigger.Trigger(cluster, target);

        if (!TryComp<PhysicsComponent>(cluster, out var physics))
            return;

        var direction = new Vector2(_random.NextFloat(-2f, 2f), _random.NextFloat(-2f, 2f));
        _physics.ApplyLinearImpulse(cluster, direction, body: physics);
    }

    private void SmokeGrenade(EntityUid target)
    {
        var ent = Spawn("Smoke", Transform(target).Coordinates);

        if (!TryComp<SmokeComponent>(ent, out var smokeComponent))
            return;

        _smoke.StartSmoke(ent, new Solution(), 40f, 50, smokeComponent);
    }

    private void Rejuvenate(EntityUid target)
    {
        _rejuvenate.PerformRejuvenate(target);
    }
}
