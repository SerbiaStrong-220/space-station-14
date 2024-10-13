using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.PkaModification;
using Content.Shared.Tag;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.PkaModification
{
    internal class PkaModificationSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        public override void Initialize()
        {

            base.Initialize();

            SubscribeLocalEvent<PkaModificationComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<PkaModificationComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<PkaModificationComponent, EntInsertedIntoContainerMessage>(OnInteractUsing);
        }


        private void OnComponentInit(Entity<PkaModificationComponent> ent, ref ComponentInit args)
        {

            _itemSlots.AddItemSlot(ent, "ModificationSlot1", ent.Comp.ModificationSlot1);
            _itemSlots.AddItemSlot(ent, "ModificationSlot2", ent.Comp.ModificationSlot2);
            _itemSlots.AddItemSlot(ent, "ModificationSlot3", ent.Comp.ModificationSlot3);
            _itemSlots.AddItemSlot(ent, "ModificationSlot4", ent.Comp.ModificationSlot4);

            if (!TryComp<RechargeBasicEntityAmmoComponent>(ent, out var rechargeComp))
                return;
            ent.Comp.StartCooldownTime = rechargeComp.RechargeCooldown;
        }

        private void OnComponentRemove(Entity<PkaModificationComponent> ent, ref ComponentRemove args)
        {
            _itemSlots.RemoveItemSlot(ent, ent.Comp.ModificationSlot1);
            _itemSlots.RemoveItemSlot(ent, ent.Comp.ModificationSlot2);
            _itemSlots.RemoveItemSlot(ent, ent.Comp.ModificationSlot3);
            _itemSlots.RemoveItemSlot(ent, ent.Comp.ModificationSlot4);

        }

        private static readonly string[] SlotNames = { "ModificationSlot1", "ModificationSlot2", "ModificationSlot3", "ModificationSlot4" };

        private void OnInteractUsing(Entity<PkaModificationComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            ent.Comp.CountCooldown = 0;
            ent.Comp.CountDamage = 0;
            ent.Comp.CountRange = 0;

            foreach (var slotName in SlotNames)
            {
                if (_itemSlots.TryGetSlot(ent, slotName, out var slot))
                    CountModules(ent, slot);
            }
            ChooseProto(ent);
        }

        private void CountModules(EntityUid ent, ItemSlot slot)
        {
            if (!TryComp<PkaModificationComponent>(ent, out var count))
                return;

            if (!HasComp<PkaModuleComponent>(slot.Item))
                return;

            if (_tag.HasTag((EntityUid)slot.Item, "ModuleCooldown"))
                count.CountCooldown++;
            if (_tag.HasTag((EntityUid)slot.Item, "ModuleRange"))
                count.CountRange++;
            if (_tag.HasTag((EntityUid)slot.Item, "ModuleDamage"))
                count.CountDamage++;
        }

        private void ChooseProto(EntityUid ent)
        {
            if (!TryComp<PkaModificationComponent>(ent, out var pkaModif))
                return;

            if (!TryComp<BasicEntityAmmoProviderComponent>(ent, out var ammoComp))
                return;

            if (!TryComp<RechargeBasicEntityAmmoComponent>(ent, out var rechargeComp))
                return;

            rechargeComp.RechargeCooldown = pkaModif.StartCooldownTime - pkaModif.CountCooldown * pkaModif.ChangePerCDModule;

            ammoComp.Proto = "BulletKinetic_" + pkaModif.CountRange.ToString() + pkaModif.CountDamage.ToString(); 
        }
    }
}
