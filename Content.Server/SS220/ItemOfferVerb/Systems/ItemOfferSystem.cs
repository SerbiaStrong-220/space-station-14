// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.SS220.ItemOfferVerb.Components;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.SS220.ItemOfferVerb.Systems
{
    public sealed class ItemOfferSystem : EntitySystem
    {
        [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
        [Dependency] private readonly EntityManager _entMan = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly HandsSystem _hands = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(AddOfferVerb);
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
                    // This is bullshit-code.
                    if (!TryComp<ItemReceiverComponent>(uid, out var itemReceiverComponent))
                    {
                        _alerts.ShowAlert(uid, AlertType.ItemOffer);
                        _popupSystem.PopupEntity($"{Name(args.User)} протягивает {Name(args.Hands.ActiveHandEntity!.Value)} {Name(uid)}", args.User, PopupType.Small);
                        var newcomp = _entMan.AddComponent<ItemReceiverComponent>(uid);
                        newcomp.Giver = args.User;
                        newcomp.Item = args.Hands.ActiveHandEntity;
                    }
                    else
                    {
                        itemReceiverComponent.Giver = args.User;
                        itemReceiverComponent.Item = args.Hands.ActiveHandEntity;
                    };
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
                _alerts.ClearAlert(receiver, AlertType.ItemOffer);
                _entMan.RemoveComponent<ItemReceiverComponent>(receiver);
            };
        }

        private bool FindFreeHand(HandsComponent component, [NotNullWhen(true)] out string? freeHand)
        {
            return (freeHand = component.GetFreeHandNames().Any() ? component.GetFreeHandNames().First() : null) != null;
        }
    }
}
