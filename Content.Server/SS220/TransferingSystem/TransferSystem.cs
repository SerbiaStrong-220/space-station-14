using Content.Shared.Hands.Components;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using System.Linq;

namespace Content.Server.SS220.Transfer
{
    internal class TransferSystem : EntitySystem
    {
        [Dependency] private readonly EntityManager _entitymanager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(AddTransferVerb);
        }

        private void AddTransferVerb(EntityUid uid, HandsComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (!args.CanInteract || args.Hands == null || args.Hands.ActiveHandEntity == null || args.Target == args.User)
                return;
            EquipmentVerb verb = new()
            {
                Act = () => TransferItemsInHands(uid, component)
            };
            args.Verbs.Add(verb);
        }

        private void TransferItemsInHands(EntityUid uid, HandsComponent component)
        {
            var ev = new StrippingSlotButtonPressed(FindFreeHand(uid, component), true);
            ev.Entity = uid;
            RaiseLocalEvent(uid, ev);
        }

        private string FindFreeHand(EntityUid target, HandsComponent component)
        {
            if (_entitymanager.TryGetComponent<HandsComponent>(target, out component!))
            {
                return component.GetFreeHandNames().First();
            };
            return "";
        }
    }
}
