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

/*
_popup.PopupEntity(alert, target);
_handbyhandsystem.TryGiveItem(user, target, item);
*/


//Text = Loc.GetString("action-transfer-name"),

//var alert = Loc.GetString("action-transfer-alert", ("user", Identity.Name(args.User, EntityManager)), ("item", args.Hands!.ActiveHandEntity!));
//_popup.PopupEntity(alert, uid);

// RaiseLocalEvent<>

/*
        public void OnUseTransferVerb(EntityUid entity, HandsComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || args.Hands == null || args.Hands.ActiveHandEntity == null)
                return;

            var userHands = Comp<HandsComponent>(args.User);
            var item = userHands.ActiveHandEntity!.Value;
            var user = args.User;
            var target = args.Target;

            PopupTransferVerb(user, target, item, args);
        }

        // EntityUid target, StrippableComponent component, StrippingSlotButtonPressed args
        // StrippingSlotButtonPressed

        public void PopupTransferVerb(EntityUid user, EntityUid target, EntityUid item, GetVerbsEvent<EquipmentVerb> args)
        {
            var alert = Loc.GetString("action-transfer-alert", ("user", Identity.Name(args.User, EntityManager)), ("item", item));
            EquipmentVerb verb = new();
            verb.Act = () =>
            {
                
            };
            verb.Text = Loc.GetString("action-transfer-name");

            args.Verbs.Add(verb);
        }
 */
