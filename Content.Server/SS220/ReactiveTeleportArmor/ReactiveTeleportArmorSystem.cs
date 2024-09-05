using System.Linq;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map.Components;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Explosion.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;




namespace Content.Server.SS220.ReactiveTeleportArmor
{
    internal class ReactiveTeleportArmorSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] protected readonly DamageableSystem Damageable = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;


        private EntityQuery<PhysicsComponent> _physicsQuery;
        private HashSet<Entity<MapGridComponent>> _targetGrids = [];

        public override void Initialize()
        {
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            base.Initialize();
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, DamageChangedEvent>(OnReactiveTeleportArmor);
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, ClothingGotUnequippedEvent>(OnUnequip);
        }

        private void OnEquip(Entity<ReactiveTeleportArmorComponent> ent, ref ClothingGotEquippedEvent args)
        {
           EnsureComp<ReactiveTeleportArmorComponent>(args.Wearer);
           ent.Comp.ArmorUid = ent;
        }
        private void OnUnequip(Entity<ReactiveTeleportArmorComponent> ent, ref ClothingGotUnequippedEvent args)
        {
            RemComp<ReactiveTeleportArmorComponent>(args.Wearer);
        }


        private void OnReactiveTeleportArmor(Entity<ReactiveTeleportArmorComponent> ent, ref DamageChangedEvent args)
        {


            if (!TryComp<ReactiveTeleportArmorComponent>(ent, out var armor))
                return;

            // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
            // This can for example happen when the user is cuffed and being pulled.
            if (TryComp<PullableComponent>(ent.Owner, out var pull) && _pullingSystem.IsPulled(ent.Owner, pull))
                _pullingSystem.TryStopPull(ent.Owner, pull);


            var xform = Transform(ent.Owner);
            var targetCoords = SelectRandomTileInRange(xform, armor.TeleportRadius);


            if (!args.DamageIncreased || args.DamageDelta == null)
                return;

            if (args.DamageDelta.GetTotal() >= ent.Comp.WakeThreshold && targetCoords != null)
            {

                switch (_random.Prob(ent.Comp.TeleportChance))
                {
                    case true:

                        _xform.SetCoordinates(ent.Owner, targetCoords.Value);
                        _audio.PlayPvs(armor.TeleportSound, ent.Owner);
                        SelectRandomTileInRange(xform, armor.TeleportRadius);

                        break;
                    case false:
                        _explosion.TriggerExplosive(ent.Comp.ArmorUid); ///пользуясб случаем я хочу сказать: ГНОМА Я ЛЮБЛЮ ТЕБЯ ПОФИКСИ ПОЖАЛУЙСТА ЭТО ХУЙНЯ
                        break;

                }
            }
        }

        private EntityCoordinates? SelectRandomTileInRange(TransformComponent userXform, float radius)
        {
            var userCoords = userXform.Coordinates.ToMap(EntityManager, _xform);
            _targetGrids.Clear();
            _lookupSystem.GetEntitiesInRange(userCoords, radius, _targetGrids);
            Entity<MapGridComponent>? targetGrid = null;

            if (_targetGrids.Count == 0)
                return null;

            // Give preference to the grid the entity is currently on.
            // This does not guarantee that if the probability fails that the owner's grid won't be picked.
            // In reality the probability is higher and depends on the number of grids.
            if (userXform.GridUid != null && TryComp<MapGridComponent>(userXform.GridUid, out var gridComp))
            {
                var userGrid = new Entity<MapGridComponent>(userXform.GridUid.Value, gridComp);
                if (_random.Prob(0.5f))
                {
                    _targetGrids.Remove(userGrid);
                    targetGrid = userGrid;
                }
            }

            if (targetGrid == null)
                targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

            EntityCoordinates? targetCoords = null;

            do
            {
                var valid = false;

                var range = (float)Math.Sqrt(radius);
                var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
                var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Value.Owner, targetGrid.Value.Comp, box, false);
                var tileList = new ValueList<Vector2i>();

                while (tilesInRange.MoveNext(out var tile))
                {
                    tileList.Add(tile.GridIndices);
                }

                while (tileList.Count != 0)
                {
                    var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
                    valid = true;
                    foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Value.Owner, targetGrid.Value.Comp,
                                 tile))
                    {
                        if (!_physicsQuery.TryGetComponent(entity, out var body))
                            continue;

                        if (body.BodyType != BodyType.Static ||
                            !body.Hard ||
                            (body.CollisionLayer & (int)CollisionGroup.MobMask) == 0)
                            continue;

                        valid = false;
                        break;
                    }

                    if (valid)
                    {
                        targetCoords = new EntityCoordinates(targetGrid.Value.Owner,
                            _mapSystem.TileCenterToVector(targetGrid.Value, tile));
                        break;
                    }
                }

                if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                    break;

                targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
            } while (true);

            return targetCoords;
        }

    }


}
