using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Server.Transfer
{
    internal class TransferSystem : EntitySystem
    {
        [Dependency] private readonly HandToHandSystem _handbyhandsystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(OnUseTransferVerb);
        }
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

        public async void PopupTransferVerb(EntityUid user, EntityUid target, EntityUid item, GetVerbsEvent<EquipmentVerb> args)
        {
            var alert = Loc.GetString("action-transfer-alert", ("user", Identity.Name(args.User, EntityManager)), ("item", item));
            EquipmentVerb verb = new();
            verb.Act = () =>
            {
                _popup.PopupEntity(alert, target);
                _handbyhandsystem.TryGiveItem(user, target, item);

            };
            verb.Text = Loc.GetString("action-transfer-name");

            args.Verbs.Add(verb);
        }
    }
}
