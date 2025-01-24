// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Map.Components;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.SS220.ReactiveTeleportArmor
{
    internal class ReactiveTeleportArmorSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
        [Dependency] private readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private readonly ExplosionSystem _explosion = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly ItemToggleSystem _toggle = default!;

        private EntityQuery<PhysicsComponent> _physicsQuery;
        private HashSet<Entity<MapGridComponent>> _targetGrids = [];

        public override void Initialize()
        {
            _physicsQuery = GetEntityQuery<PhysicsComponent>();

            base.Initialize();
            SubscribeLocalEvent<TeleportOnDamageComponent, DamageChangedEvent>(TeleporWhenDamaged);
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, ClothingGotUnequippedEvent>(OnUnequip);
            SubscribeLocalEvent<ReactiveTeleportArmorComponent, ItemToggledEvent>(ToggleDone);
        }

        private void OnEquip(Entity<ReactiveTeleportArmorComponent> ent, ref ClothingGotEquippedEvent args)
        {
            EnsureComp<TeleportOnDamageComponent>(args.Wearer, out var comp);
            comp.SavedUid = ent;
        }

        private void OnUnequip(Entity<ReactiveTeleportArmorComponent> ent, ref ClothingGotUnequippedEvent args)
        {
            RemComp<TeleportOnDamageComponent>(args.Wearer);
        }

        private void ToggleDone(Entity<ReactiveTeleportArmorComponent> ent, ref ItemToggledEvent args)
        {
            var prefix = args.Activated ? "on" : null;
            _item.SetHeldPrefix(ent, prefix);
            _clothing.SetEquippedPrefix(ent, prefix);
        }


        private void TeleporWhenDamaged(Entity<TeleportOnDamageComponent> ent, ref DamageChangedEvent args)
        {
            if (!TryComp<TeleportOnDamageComponent>(ent, out var armor))
                return;

            var xform = Transform(ent.Owner);
            var targetCoords = SelectRandomTileInRange(xform, armor.TeleportRadius);

            if (!args.DamageIncreased || args.DamageDelta == null)
                return;
            ///teleport entity if taken damage && coord = !null && armor is on && !cooldown
            if (args.DamageDelta.GetTotal() >= ent.Comp.WakeThreshold && targetCoords != null && _toggle.IsActivated(ent.Comp.SavedUid) && !ent.Comp.OnCoolDown)
            {
                ent.Comp.OnCoolDown = true;

                // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
                // This can for example happen when the user is cuffed and being pulled.
                if (TryComp<PullableComponent>(ent.Owner, out var pull) && _pullingSystem.IsPulled(ent.Owner, pull))
                    _pullingSystem.TryStopPull(ent.Owner, pull);

                if (_random.Prob(ent.Comp.TeleportChance))
                {
                    _xform.SetCoordinates(ent.Owner, targetCoords.Value);
                    _audio.PlayPvs(armor.TeleportSound, ent.Owner);
                    SelectRandomTileInRange(xform, armor.TeleportRadius);
                }
                else
                {
                    _explosion.TriggerExplosive(ent.Comp.SavedUid);
                }
                Timer.Spawn(ent.Comp.CoolDownTime, () => ent.Comp.OnCoolDown = false);
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

            if (!valid || _targetGrids.Count != 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

            return targetCoords;

        }
    }
}
