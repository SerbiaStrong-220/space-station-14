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


        private EntityQuery<PhysicsComponent> _physicsQuery;
        private HashSet<Entity<MapGridComponent>> _targetGrids = [];

        public override void Initialize()
        {
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            base.Initialize();
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, DamageChangedEvent>(OnReactiveTeleportArmor);
        }

        private void OnReactiveTeleportArmor(EntityUid uid, ReactiveTeleportArmorComponent component, ref DamageChangedEvent args)
        {
            if (component.ArmorEntity is not { } entity)
                return;
            if (!TryComp<ReactiveTeleportArmorComponent>(uid, out var armor))
                return;

            // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
            // This can for example happen when the user is cuffed and being pulled.
            if (TryComp<PullableComponent>(entity, out var pull) && _pullingSystem.IsPulled(entity, pull))
                _pullingSystem.TryStopPull(entity, pull);

            if (!args.DamageIncreased || args.DamageDelta == null)
                return;


            var xform = Transform(entity);
            var targetCoords = SelectRandomTileInRange(xform, armor.TeleportRadius);

            if (args.DamageIncreased)
            {
                SelectRandomTileInRange(xform, armor.TeleportRadius);
            }

            if (targetCoords != null)
            {
                _xform.SetCoordinates(entity, targetCoords.Value);
                _audio.PlayPvs(armor.TeleportSound, entity);
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
