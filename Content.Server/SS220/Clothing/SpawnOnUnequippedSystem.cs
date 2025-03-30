using Content.Shared.Administration.Logs;
using Content.Shared.Inventory.Events;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Random;
using static Content.Shared.Storage.EntitySpawnCollection;
using Content.Shared.Clothing.Components;

namespace Content.Server.SS220.Clothing
{
    public sealed class LimitiedEquipSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<LimitiedEquipComponent, GotUnequippedEvent>(OnGotUnequipped);
        }
        private void OnGotUnequipped(Entity<LimitiedEquipComponent> uid, ref GotUnequippedEvent args)
        {

            // If starting with zero or less uses, this component is a no-op
            if (!TryComp<ClothingComponent>(uid, out var clothComp))
                return;

            if (clothComp.Slots != args.SlotFlags)
                return;

            if (uid.Comp.Uses <= 0)
                return;

            var coords = Transform(args.Equipee).Coordinates;
            var spawnEntities = GetSpawns(uid.Comp.Items, _random);
            EntityUid? entityToPlaceInHands = null;

            foreach (var proto in spawnEntities)
            {
                entityToPlaceInHands = Spawn(proto, coords);
                _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.Equipee)} unequipped {ToPrettyString(uid)} which spawned {ToPrettyString(entityToPlaceInHands.Value)}");
            }

            uid.Comp.Uses--;

            // Delete entity only if component was successfully used
            if (uid.Comp.Uses <= 0)
                EntityManager.QueueDeleteEntity(uid);

            if (entityToPlaceInHands != null)
                _hands.PickupOrDrop(args.Equipee, entityToPlaceInHands.Value);
        }
    }
}
