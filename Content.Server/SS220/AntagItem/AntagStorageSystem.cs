using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.Storage.EntitySystems;
using Content.Server.Strip;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Storage.Components;

namespace Content.Server.SS220.AntagItem
{
    public sealed partial class AntagStorageSystem : EntitySystem
    {
        [Dependency] private readonly StorageSystem _storage = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly MobStateSystem _mob = default!;
        [Dependency] private readonly StrippableSystem _strippable = default!;
        public override void Initialize()
        {
            SubscribeLocalEvent<AntagStorageComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
            SubscribeLocalEvent<HumanoidAppearanceComponent, MobStateChangedEvent>(OnDeath);
        }

        private void OnDeath(EntityUid uid, HumanoidAppearanceComponent component, MobStateChangedEvent ev)
        {
            TryComp<InventoryComponent>(uid, out var inventory);
            _inventory.TryGetSlotEntity(uid, "back", out var backUid, inventory);
            if (!TryComp<AntagStorageComponent>(backUid, out var antagStorageComponent))
                return;
            if (ev.NewMobState == MobState.Dead && HasComp<AntagStorageComponent>(backUid))
            {
                _inventory.TryUnequip(uid, antagStorageComponent.Slot, false, false, false, inventory);
            }
        }

        private void OnOpenAttempt(EntityUid uid, AntagStorageComponent component, StorageOpenAttemptEvent ev)
        {
            // уээээээ
        }
    }
}
