using Content.Server.SS220.Events;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using System.Globalization;

namespace Content.Server.Inventory
{
    public sealed class ServerInventorySystem : InventorySystem
    {

        [Dependency] private readonly SharedIdCardSystem _sharedIdCard = default!; // SS220 Borgs-Id-fix

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InventoryComponent, BeforeExplodeEvent>(OnExploded);

            SubscribeLocalEvent<InventoryComponent, GetInsteadIdCardNameEvent>(OnGetIdCardName); // SS220 Borgs-Id-fix
        }

        private void OnExploded(Entity<InventoryComponent> ent, ref BeforeExplodeEvent args)
        {
            // explode each item in their inventory too
            var slots = new InventorySlotEnumerator(ent);
            while (slots.MoveNext(out var slot))
            {
                if (slot.ContainedEntity != null)
                    args.Contents.Add(slot.ContainedEntity.Value);
            }
        }

        public void TransferEntityInventories(Entity<InventoryComponent?> source, Entity<InventoryComponent?> target)
        {
            if (!Resolve(source.Owner, ref source.Comp) || !Resolve(target.Owner, ref target.Comp))
                return;

            var enumerator = new InventorySlotEnumerator(source.Comp);
            while (enumerator.NextItem(out var item, out var slot))
            {
                if (TryUnequip(source, slot.Name, true, true, inventory: source.Comp, triggerHandContact: true))
                    TryEquip(target, item, slot.Name , true, true, inventory: target.Comp, triggerHandContact: true);
            }
        }

        // SS220 Borgs-Id-fix start
        private void OnGetIdCardName(EntityUid uid, InventoryComponent invent, ref GetInsteadIdCardNameEvent args)
        {
            var idCard = new Entity<IdCardComponent>();
            var textInfo = CultureInfo.CurrentCulture.TextInfo;
            string idCardName = Loc.GetString("chat-radio-no-id");
            idCardName = textInfo.ToTitleCase(idCardName);
            if (TryGetSlotEntity(uid, "id", out var idUid) && _sharedIdCard.TryGetIdCard(idUid.Value, out idCard))
                args.Name = idCard.Comp.LocalizedJobTitle ?? idCardName;
            else
                args.Name = idCardName;
        }
        // SS220 Borgs-Id-fix end
    }
}
