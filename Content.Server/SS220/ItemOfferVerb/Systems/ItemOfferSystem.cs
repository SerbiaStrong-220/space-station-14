// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.SS220.ItemOfferVerb.Components;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.ItemOfferVerb;
using Content.Shared.Verbs;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Interaction.Components;
using Robust.Shared.Input.Binding;
using Content.Shared.SS220.Input;

namespace Content.Server.SS220.ItemOfferVerb.Systems
{
    public sealed class ItemOfferSystem : EntitySystem
    {
        [Dependency] private readonly EntityManager _entMan = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly HandsSystem _hands = default!;

        [ValidatePrototypeId<AlertPrototype>]
        private const string ItemOfferAlert = "ItemOffer";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(AddOfferVerb);
            SubscribeLocalEvent<ItemReceiverComponent, ItemOfferAlertEvent>(OnItemOffserAlertClicked);

            CommandBinds.Builder
                .Bind(KeyFunctions220.ItemOffer,
                    new PointerInputCmdHandler(HandleItemOfferKey))
                .Register<ItemOfferSystem>();
        }

        private bool HandleItemOfferKey(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!args.EntityUid.IsValid() || !EntityManager.EntityExists(args.EntityUid))
                return false;

            if (args.Session?.AttachedEntity == null)
                return false;

            DoItemOffer(args.Session.AttachedEntity.Value, args.EntityUid);
            return true;
        }

        private void OnItemOffserAlertClicked(Entity<ItemReceiverComponent> ent, ref ItemOfferAlertEvent args)
        {
            TransferItemInHands(ent, ent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var enumerator = EntityQueryEnumerator<ItemReceiverComponent, TransformComponent>();
            while (enumerator.MoveNext(out var uid, out var comp, out var transform))
            {
                var receiverPos = Transform(comp.Giver).Coordinates;
                var giverPos = Transform(uid).Coordinates;
                receiverPos.TryDistance(EntityManager, giverPos, out var distance);
                var giverHands = Comp<HandsComponent>(comp.Giver);
                if (distance > comp.ReceiveRange)
                {
                    _alerts.ClearAlert(uid, ItemOfferAlert);
                    _entMan.RemoveComponent<ItemReceiverComponent>(uid);
                }
                //FunTust: added a new variable responsible for whether the object is still in the hand during transmission
                var foundInHand = false;
                foreach (var hand in giverHands.Hands)
                {
                    if (hand.Value.Container!.Contains(comp.Item!.Value))
                        //break;
                        //FunTust: Now we check all hands and if found, we change the value of the variable
                        foundInHand = true;
                    /*
                     FunTust: Actually, what caused the error was that if the object was in the second hand,
                    then when we checked the first hand we didn't find it and deleted the transfer request.
                    _alerts.ClearAlert(uid, AlertType.ItemOffer);
                    _entMan.RemoveComponent<ItemReceiverComponent>(uid);
                    */
                }
                //FunTust: Just moved it here with a variable check, maybe not the most elegant solution,
                //but it should work and it shouldn't affect performance too much because there are only 2 hands.
                if (!foundInHand)
                {
                    _alerts.ClearAlert(uid, ItemOfferAlert);
                    _entMan.RemoveComponent<ItemReceiverComponent>(uid);
                }
            }
        }

        private void AddOfferVerb(EntityUid uid, HandsComponent component, GetVerbsEvent<EquipmentVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || args.Hands == null || args.Hands.ActiveHandEntity == null)
                return;

            EquipmentVerb verb = new EquipmentVerb()
            {
                Text = "Передать предмет",
                Act = () =>
                {
                   DoItemOffer(args.User, uid);
                },
            };

            args.Verbs.Add(verb);
        }
        public void TransferItemInHands(EntityUid receiver, ItemReceiverComponent? itemReceiver)
        {
            if (itemReceiver == null)
                return;
            _hands.PickupOrDrop(itemReceiver.Giver, itemReceiver.Item!.Value);
            if (_hands.TryPickupAnyHand(receiver, itemReceiver.Item!.Value))
            {
                var loc = Loc.GetString("loc-item-offer-transfer",
                    ("user", itemReceiver.Giver),
                    ("item", itemReceiver.Item),
                    ("target", receiver));
                _popupSystem.PopupEntity(loc, itemReceiver.Giver, PopupType.Medium);
                _alerts.ClearAlert(receiver, ItemOfferAlert);
                _entMan.RemoveComponent<ItemReceiverComponent>(receiver);
            };
        }
        private bool FindFreeHand(HandsComponent component, [NotNullWhen(true)] out string? freeHand)
        {
            return (freeHand = component.GetFreeHandNames().Any() ? component.GetFreeHandNames().First() : null) != null;
        }

        private void DoItemOffer(EntityUid user, EntityUid target)
        {
            if (!TryComp<HandsComponent>(target, out var handsComponent))
                return;

            // (fix https://github.com/SerbiaStrong-220/space-station-14/issues/2054)
            if (HasComp<BorgChassisComponent>(user) || !FindFreeHand(handsComponent, out _) || target == user )
                return;

            if (!_hands.TryGetActiveItem(user, out var item))
                return;

            if (HasComp<UnremoveableComponent>(item))
                return;

            var itemReceiver = EnsureComp<ItemReceiverComponent>(target);
            itemReceiver.Giver = user;
            itemReceiver.Item = item;
            _alerts.ShowAlert(target, ItemOfferAlert);

            var loc = Loc.GetString("loc-item-offer-attempt",
                ("user", user),
                ("item", item),
                ("target", target));
            _popupSystem.PopupEntity(loc, user);
          }
    }
}
