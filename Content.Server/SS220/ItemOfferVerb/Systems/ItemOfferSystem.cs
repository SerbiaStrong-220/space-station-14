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
            if (!args.CanInteract || !args.CanAccess || args.Hands == null || args.Hands.ActiveHandEntity == null
                || args.Target == args.User || !FindFreeHand(component, out var freeHand))
                return;

            EquipmentVerb verb = new EquipmentVerb()
            {
                Text = "Передать предмет",
                Act = () =>
                {
                    var itemReceiver = EnsureComp<ItemReceiverComponent>(uid);
                    itemReceiver.Giver = args.User;
                    itemReceiver.Item = args.Hands.ActiveHandEntity;
                    _alerts.ShowAlert(uid, ItemOfferAlert);
                    _popupSystem.PopupEntity($"{Name(args.User)} протягивает {Name(args.Hands.ActiveHandEntity!.Value)} {Name(uid)}", args.User, PopupType.Small);
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
                _popupSystem.PopupEntity($"{Name(itemReceiver.Giver)} передал {Name(itemReceiver.Item!.Value)} {Name(receiver)}!", itemReceiver.Giver, PopupType.Medium);
                _alerts.ClearAlert(receiver, ItemOfferAlert);
                _entMan.RemoveComponent<ItemReceiverComponent>(receiver);
            };
        }
        private bool FindFreeHand(HandsComponent component, [NotNullWhen(true)] out string? freeHand)
        {
            return (freeHand = component.GetFreeHandNames().Any() ? component.GetFreeHandNames().First() : null) != null;
        }
    }
}
