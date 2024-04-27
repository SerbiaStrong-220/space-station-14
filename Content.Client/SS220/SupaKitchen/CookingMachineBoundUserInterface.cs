// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Chemistry.Reagent;
using Content.Shared.SS220.SupaKitchen;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.SupaKitchen.UI
{
    [UsedImplicitly]
    public sealed class CookingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CookingMachineWindow? _menu;

        [ViewVariables]
        private readonly Dictionary<int, EntityUid> _solids = new();

        [ViewVariables]
        private readonly Dictionary<int, ReagentQuantity> _reagents = new();

        private IEntityManager _entManager;

        public CookingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _entManager = IoCManager.Resolve<IEntityManager>();
        }

        protected override void Open()
        {
            base.Open();
            _menu = new CookingMachineWindow(this);
            _menu.OpenCentered();
            _menu.OnClose += Close;
            _menu.StartButton.OnPressed += _ => SendMessage(new CookingMachineStartCookMessage());
            _menu.EjectButton.OnPressed += _ => SendMessage(new CookingMachineEjectMessage());
            _menu.IngredientsList.OnItemSelected += args =>
            {
                SendMessage(new CookingMachineEjectSolidIndexedMessage(_entManager.GetNetEntity(_solids[args.ItemIndex])));
            };

            _menu.OnCookTimeSelected += (args, buttonIndex) =>
            {
                var actualButton = (CookingMachineWindow.MicrowaveCookTimeButton) args.Button;
                SendMessage(new CookingMachineSelectCookTimeMessage(buttonIndex, actualButton.CookTime));
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            _solids.Clear();
            _menu?.Dispose();
        }


        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (state is not CookingMachineUpdateUserInterfaceState cState)
            {
                return;
            }

            _menu?.ToggleBusyDisableOverlayPanel(cState.IsMachineBusy);
            RefreshContentsDisplay(_entManager.GetEntityArray(cState.ContainedSolids));

            if (_menu == null) return;

            var currentlySelectedTimeButton = (Button) _menu.CookTimeButtonVbox.GetChild(cState.ActiveButtonIndex);
            currentlySelectedTimeButton.Pressed = true;
            var cookTime = cState.ActiveButtonIndex == 0
                ? Loc.GetString("cooking-machine-menu-instant-button")
                : cState.CurrentCookTime.ToString();
            _menu.CookTimeInfoLabel.Text = Loc.GetString("cooking-machine-bound-user-interface-cook-time-label",
                                                         ("time", cookTime));

            _menu.EjectButton.Visible = !cState.EjectUnavailable;
        }

        private void RefreshContentsDisplay(EntityUid[] containedSolids)
        {
            _reagents.Clear();

            if (_menu == null) return;

            _solids.Clear();
            _menu.IngredientsList.Clear();
            foreach (var entity in containedSolids)
            {
                if (EntMan.Deleted(entity))
                {
                    return;
                }

                // TODO just use sprite view

                Texture? texture;
                if (EntMan.TryGetComponent<IconComponent>(entity, out var iconComponent))
                {
                    texture = EntMan.System<SpriteSystem>().GetIcon(iconComponent);
                }
                else if (EntMan.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
                {
                    texture = spriteComponent.Icon?.Default;
                }
                else
                {
                    continue;
                }

                var solidItem = _menu.IngredientsList.AddItem(EntMan.GetComponent<MetaDataComponent>(entity).EntityName, texture);
                var solidIndex = _menu.IngredientsList.IndexOf(solidItem);
                _solids.Add(solidIndex, entity);
            }
        }
    }
}
