using Content.Shared.Containers.ItemSlots;
using Content.Shared.PDA;
using Content.Shared.SS220.Spray.Components;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Spray.Systems
{
    public abstract class SharedSprayVisualsSystem : EntitySystem
    {
        [Dependency] protected readonly ItemSlotsSystem ItemSlotsSystem = default!;
        [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TankVisualsComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<TankVisualsComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<TankVisualsComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
            SubscribeLocalEvent<TankVisualsComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        }
        protected virtual void OnComponentInit(EntityUid uid, TankVisualsComponent tank, ComponentInit args)
        {
            ItemSlotsSystem.AddItemSlot(uid, TankVisualsComponent.TankNozzleSlot, tank.TankSlot);

            UpdateTankAppearance(uid, tank);

        }

        private void OnComponentRemove(EntityUid uid, TankVisualsComponent tank, ComponentRemove args)
        {
            ItemSlotsSystem.RemoveItemSlot(uid, tank.TankSlot);
        }

        protected virtual void OnItemInserted(EntityUid uid, TankVisualsComponent tank, EntInsertedIntoContainerMessage args)
        {
            if (args.Container.ID == PdaComponent.PdaIdSlotId)
                tank.ContainedNozzle = args.Entity;

            UpdateTankAppearance(uid, tank);
        }

        protected virtual void OnItemRemoved(EntityUid uid, TankVisualsComponent tank, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == tank.TankSlot.ID)
                tank.ContainedNozzle = null;

            UpdateTankAppearance(uid, tank);
        }

        private void UpdateTankAppearance(EntityUid uid, TankVisualsComponent tank)
        {
            Appearance.SetData(uid, TankVisuals.NozzleInserted, tank.ContainedNozzle != null);
        }

    }
}
