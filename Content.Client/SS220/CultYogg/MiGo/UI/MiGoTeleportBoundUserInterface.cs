// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.CultYogg.MiGo.UI
{
    public sealed class MiGoTeleportBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
    {
        [ViewVariables]
        private MiGoTeleportMenu? _menu;

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindowCenteredLeft<MiGoTeleportMenu>();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;//ToDo_SS220 change this
            _menu.OnItemSelected += OnItemSelected;
            Refresh();
        }

        public void Refresh()
        {
            /*
            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(_cachedInventory, enabled);
            */
        }

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;
            _menu.OnClose -= Close;
        }
    }
}
