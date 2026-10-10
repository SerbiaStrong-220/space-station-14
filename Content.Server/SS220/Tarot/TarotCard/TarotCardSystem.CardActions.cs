using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Anomaly.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Dataset;
using Content.Shared.Doors.Components;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Content.Shared.Prototypes;
using Content.Shared.SS220.EntityBlockDamage;
using Content.Shared.SS220.EntityEffects;
using Content.Shared.SS220.HereticAbilities;
using Content.Shared.Traits.Assorted;
using Content.Shared.VendingMachines;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.SS220.Tarot.TarotCard;

/// <summary>
/// Contains only actions for Tarot Cards
/// </summary>
public sealed partial class TarotCardSystem
{
    // fool card start
    private void TeleportToArrivals(EntityUid target, EntityUid? user)
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

    private void ClearInventory(EntityUid target, EntityUid? user)
    {
        if (!TryComp<InventoryComponent>(target, out var inventoryComponent))
            return;

        foreach (var container in inventoryComponent.Containers)
        {
            if (container.ContainedEntity != null)
            {
                _container.Remove(container.ContainedEntity.Value, container);
            }
        }
    }
    // fool card end

    // magician card start
    private void OpenNearestAirlock(EntityUid target, EntityUid? user)
    {
        var doorList = _lookup.GetEntitiesInRange<AirlockComponent>(Transform(target).Coordinates, 3f).ToList();

        if (doorList.Count == 0)
            return;

        var doorEntity = _random.Pick(doorList);
        _door.StartOpening(doorEntity);
    }

    private void PushPlayers(EntityUid target, EntityUid? user)
    {
        var lookup = _lookup.GetEntitiesInRange(target, 4f, LookupFlags.Dynamic | LookupFlags.Sundries);
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in lookup)
        {
            if (physQuery.TryGetComponent(ent, out var phys)
                && (phys.CollisionMask & (int) CollisionGroup.GhostImpassable) != 0)
                continue;

            var tempXform = Transform(ent);

            var direction = _transform.GetMapCoordinates(ent, xform: tempXform).Position - _transform.GetMapCoordinates(target, xform: Transform(target)).Position;
            _throwing.TryThrow(ent, direction * 2, 4f, target, 0);
        }
    }
    // magician card end

    // high-priestess card start
    private void Slowdown(EntityUid target, EntityUid? user)
    {
        var moveSpeedEffect = new MovespeedModifier
        {
            StatusLifetime = 10f,
            WalkSpeedModifier = 0.15f,
            SprintSpeedModifier = 0.15f,
        };

        moveSpeedEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void SpawnRandomAnomaly(EntityUid target, EntityUid? user)
    {
        if (HasComp<InnerBodyAnomalyComponent>(target))
            return;

        var randomAnomaly = _random.Pick(_proto.Index<DatasetPrototype>(RandomAnomalyInjectorsSpawn).Values);
        var randomEntityAnomaly = Spawn(randomAnomaly, MapCoordinates.Nullspace);

        if (!TryComp<InnerBodyAnomalyInjectorComponent>(randomEntityAnomaly, out var injectorComponent))
            return;

        EntityManager.AddComponents(target, injectorComponent.InjectionComponents);
        QueueDel(randomEntityAnomaly);
    }
    // high-priestess card end

    // empress card start
    private void TransferSolution(EntityUid target, EntityUid? user)
    {
        if (!TryComp<SolutionContainerManagerComponent>(target, out _) ||
            !_solution.TryGetSolution(target, Chemicals, out var targetSolution))
            return;

        _solution.TryAddReagent(targetSolution.Value, Omnizine, 20f);
    }

    private void EnsurePacified(EntityUid target, EntityUid? user)
    {
        var entities =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 5f);

        foreach (var entity in entities.Where(entity => entity.Owner != target))
        {
            _statusEffects.TryAddStatusEffectDuration(entity, StatusEffectPacifism, out _, TimeSpan.FromSeconds(40f));
        }
    }
    // empress card end

    // emperor card start
    private void TeleportToBridge(EntityUid target, EntityUid? user)
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

    private void TeleportToHoD(EntityUid target, EntityUid? user)
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
    // emperor card end

    // hierophant card start
    private void SpawnCatCake(EntityUid target, EntityUid? user)
    {
        var coords = Transform(target).Coordinates;
        var catCake = SpawnAtPosition(MobCatCake, coords);
        var solution = new Solution(Omnizine, FixedPoint2.New(50f));

        if (!TryComp<SolutionContainerManagerComponent>(catCake, out var containerManagerComponent))
            return;

        if (!_solution.TryGetSolution((catCake, containerManagerComponent), Food, out var solutionEnt))
            return;

        _solution.SetCapacity(solutionEnt.Value, solutionEnt.Value.Comp.Solution.Volume + solution.Volume);

        _solution.TryTransferSolution(solutionEnt.Value, solution, solution.Volume);
    }

    private void SpawnCerberus(EntityUid target, EntityUid? user)
    {
        var coords = Transform(target).Coordinates;
        var cerberus = SpawnAtPosition(MobCorgiCerberus, coords);

        _npcFaction.MakeFriendlyEntities(cerberus, target);
    }
    // hierophant card end

    // lovers card start
    private void HealTarget(EntityUid target, EntityUid? user)
    {
        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BurnGroup), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>(Blunt), -20);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>(Asphyxiation), -40);
        damageSpec += new DamageSpecifier(_proto.Index<DamageTypePrototype>(Poison), -40);

        _bloodstream.TryModifyBloodLevel(target, 100f);
        _damageable.TryChangeDamage(target, damageSpec, true);

        Dirty(target, damageableComponent);
    }

    private void HurtTarget(EntityUid target, EntityUid? user)
    {
        if(!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BurnGroup), 40);
        _stun.TryAddParalyzeDuration(target, TimeSpan.FromSeconds(4f));
        _bloodstream.TryModifyBloodLevel(target, -120f);
        _damageable.TryChangeDamage(target, damageSpec, true);

        Dirty(target, damageableComponent);
    }
    // lovers card end

    // chariot card start
    private void ApplyChariotEffects(EntityUid target, EntityUid? user)
    {
        // Pacified in 10 sec after use
        _statusEffects.TryAddStatusEffectDuration(target, StatusEffectPacifism, out _, TimeSpan.FromSeconds(10f));

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
        var genericStatusEffect = new ModifyStatusEffect
        {
            EffectProto = "Adrenaline",
            Time = 10f,
            Type = StatusEffectMetabolismType.Add,
        };

        // Increase move speed by 1.25 to target
        var moveSpeedEffect = new MovespeedModifier
        {
            StatusLifetime = 10f,
            WalkSpeedModifier = 1.25f,
            SprintSpeedModifier = 1.25f,
        };

        genericStatusEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
        moveSpeedEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void SpeedUpEntity(EntityUid target, EntityUid? user)
    {
        var moveSpeedEffect = new MovespeedModifier
        {
            StatusLifetime = 30f,
            WalkSpeedModifier = 1.5f,
            SprintSpeedModifier = 1.5f,
        };

        moveSpeedEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }
    // chariot card end

    // strength card start

    // TODO normal action for strength

    private void MassHallucinations(EntityUid target, EntityUid? user)
    {
        // TODO THIS SHIT
    }
    // strength card end

    // hermit card start
    private void TeleportToVend(EntityUid target, EntityUid? user)
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

    private void TransformGuns(EntityUid target, EntityUid? user)
    {
        var guns = _lookup.GetEntitiesInRange<GunComponent>(Transform(target).Coordinates, 3f);

        foreach (var gun in guns)
        {
            var coord = Transform(gun).Coordinates;
            Spawn(SpaceCash2500, coord);
            QueueDel(gun);
        }
    }
    // hermit card end

    // wheel of fortune card start
    private void CreateGambling(EntityUid target, EntityUid? user)
    {
        var arcadeList = _proto.Index<DatasetPrototype>(RandomArcadeSpawner).Values;

        var randomArcade = _random.Pick(arcadeList);
        Spawn(randomArcade, Transform(target).Coordinates);
    }

    private void RollDieOfFortune(EntityUid target, EntityUid? user)
    {
        var randomValue = _random.Next(1, 21);

        _dieOfFate.DoAction(target, randomValue);
    }
    // wheel of fortune card end

    // justice card start
    private void SpawnJusticeItems(EntityUid target, EntityUid? user)
    {
        var coords = Transform(target).Coordinates;
        var items = _proto.Index<DatasetPrototype>(JusticeItems);

        foreach (var item in items.Values)
        {
            SpawnAtPosition(item, coords);
        }
    }

    private void SpawnRandomCrate(EntityUid target, EntityUid? user)
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
    // justice card end

    // hanged man card start
    private void TeleportToRandomTile(EntityUid target, EntityUid? user)
    {
        var randomTile = _scramOnTrigger.SelectRandomTileInRange(Transform(target), 30f);
        if (randomTile == null)
            return;

        _transform.SetCoordinates(target, randomTile.Value);
    }

    private void FixturesSet(EntityUid target, EntityUid? user)
    {
        EnsureComp<WalkThroughWallsComponent>(target, out var walkThroughWallsComponent);

        walkThroughWallsComponent.Duration = 10f;
        Dirty(target, walkThroughWallsComponent);
    }
    // hanged man card end

    // death card start
    private void HurtAnother(EntityUid target, EntityUid? user)
    {
        var lookup =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 4f);

        var damageSpec = new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BurnGroup), 20);
        damageSpec += new DamageSpecifier(_proto.Index<DamageGroupPrototype>(BruteGroup), 20);

        foreach (var entity in lookup)
        {
            _damageable.TryChangeDamage(entity, damageSpec, true);
        }
    }

    private void ModifyThreshold(EntityUid target, EntityUid? user)
    {
        var modifyThreshold = new ModifyThresholdEffect
        {
            Duration = 500f,
            NewThresholds = new Dictionary<FixedPoint2, MobState>
            {
                { 150, MobState.Critical },
            },
        };

        modifyThreshold.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }
    // death card end

    // temperance card start
    private void HealLing(EntityUid target, EntityUid? user)
    {
        RemCompDeferred<BlindableComponent>(target);
        RemCompDeferred<PermanentBlindnessComponent>(target);
        RemCompDeferred<LegsParalyzedComponent>(target);

        if (!TryComp<DamageableComponent>(target, out var damageableComponent))
            return;

        var damageSpec = new DamageSpecifier();

        foreach (var (group, value) in damageableComponent.DamagePerGroup)
        {
            if (group != ToxinGroup || value <= 0)
                continue;

            if (!_proto.TryIndex<DamageGroupPrototype>(group, out var damageType))
                continue;

            damageSpec += new DamageSpecifier(damageType, -value * damageType.DamageTypes.Count);
        }

        _damageable.TryChangeDamage(target, damageSpec);
        Dirty(target, damageableComponent);
    }

    private void EatPills(EntityUid target, EntityUid? user)
    {
        if (!TryComp<SolutionContainerManagerComponent>(target, out var targetSolutionContainer))
            return;

        if (!_solution.TryGetSolution((target, targetSolutionContainer), Chemicals, out var solComp, out _))
            return;

        var targetCoords = Transform(target).Coordinates;

        for (var i = 0; i < 3; i++)
        {
            var pill = Spawn(StrangePill, targetCoords);

            if (!TryComp<SolutionContainerManagerComponent>(pill, out var solution) ||
                !_solution.TryGetSolution((pill, solution), Food, out _, out var solutionReagent) ||
                solutionReagent.Volume == FixedPoint2.Zero)
            {
                QueueDel(pill);
                continue;
            }

            _solution.TryTransferSolution(solComp.Value, solutionReagent, solutionReagent.Volume);
            QueueDel(pill);
        }
    }
    // temperance card end

    // devil card start
    private void HelpMessage(EntityUid target, EntityUid? user)
    {
        var targetPos = Transform(target).LocalPosition;

        var allEntities =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 50f)
                .Where(e =>
                {
                    var entityPos = Transform(e).LocalPosition;
                    var distance = (entityPos - targetPos).Length();
                    return distance >= 30f;
                })
                .ToList();

        if (allEntities.Count == 0)
            return;

        var caller = _random.Pick(allEntities);
        var helpMessage = _random.Pick(_proto.Index<DatasetPrototype>(RandomMessageToChat).Values);

        _chat.TrySendInGameICMessage(caller, helpMessage, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private void ClusterFlashBang(EntityUid target, EntityUid? user)
    {
        var cluster = SpawnAtPosition(ClusterBangFull, Transform(target).Coordinates);
        _trigger.Trigger(cluster, target);

        if (!TryComp<PhysicsComponent>(cluster, out var physics))
            return;

        var direction = new Vector2(_random.NextFloat(-2f, 2f), _random.NextFloat(-2f, 2f));
        _physics.ApplyLinearImpulse(cluster, direction, body: physics);
    }
    // devil card end

    // tower card start

    // TODO tower card

    // tower card end

    // star card start
    private void GivePinpointer(EntityUid target, EntityUid? user)
    {
        var pinpointerProto = _proto.Index(PinpointerProto);
        var pinpointerEntity = SpawnAtPosition(pinpointerProto.ID, Transform(target).Coordinates);
        _hands.PickupOrDrop(target, pinpointerEntity);

        EnsureComp<TimedDespawnComponent>(pinpointerEntity, out var timedDespawnComponent);
        timedDespawnComponent.Lifetime = 10f; // now only for testing
    }

    private void TeleportToUser(EntityUid target, EntityUid? user)
    {
        if (user == null)
            return;

        _transform.SetCoordinates(target, Transform(user.Value).Coordinates);
    }
    // star card end

    // moon card start
    private void RandomTeleportation(EntityUid target, EntityUid? user)
    {
        var lookup =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 10f)
                .ToList();

        if (lookup.Count < 2) // min players number for teleport each other
            return;

        _random.Shuffle(lookup);

        var positions = new Dictionary<EntityUid, EntityCoordinates>();

        foreach (var ent in lookup)
        {
            positions[ent] = Transform(ent.Owner).Coordinates;
        }

        for (var i = 0; i < lookup.Count; i++)
        {
            var current = lookup[i];
            var nextIndex = (i + 1) % lookup.Count;
            var targetCoords = positions[lookup[nextIndex]];
            _transform.SetCoordinates(current, targetCoords);
        }
    }

    // TODO reverse action

    // moon card end

    // sun card start
    private void Rejuvenate(EntityUid target, EntityUid? user)
    {
        _rejuvenate.PerformRejuvenate(target);
    }

    private void SmokeGrenade(EntityUid target, EntityUid? user)
    {
        var ent = Spawn("Smoke", Transform(target).Coordinates);

        if (!TryComp<SmokeComponent>(ent, out var smokeComponent))
            return;

        _smoke.StartSmoke(ent, new Solution(), 40f, 50, smokeComponent);
    }
    // sun card end

    // judgment card start

    // TODO judgment card

    // judgment card end

    // world card start
    private void BlockAction(EntityUid target, EntityUid? user)
    {
        var blockMoveEffect = new BlockActionEffect
        {
            Duration = 20f, // test
        };

        blockMoveEffect.Effect(new EntityEffectBaseArgs(target, EntityManager));
    }

    private void BlockActionInRange(EntityUid target, EntityUid? user)
    {
        var entities =
            _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(target).Coordinates, 5f);

        foreach (var entity in entities)
        {
            var blockMoveEffect = new BlockActionEffect
            {
                Duration = 10f, // test
            };

            blockMoveEffect.Effect(new EntityEffectBaseArgs(entity, EntityManager));
        }
    }
    // world card end
}
