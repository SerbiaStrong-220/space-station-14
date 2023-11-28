using Content.Client.SS220.MachineStorage.SmartFridge.UI;
using Content.Client.VendingMachines.UI;
using Content.Shared.Storage;
using Robust.Shared.GameObjects;
using Content.Shared.VendingMachines;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using System.Linq;
using Content.Shared.Actions;
using Content.Client.SS220.MachineStorage.SmartFridge;
using Content.Shared.SS220.MachineStorage.SmartFridge;

namespace Content.Client.SS220.MachineStorage.SmartFridge
{
    public sealed class SmartFridgeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private SmartFridgeMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        [Dependency] private readonly IEntityManager _entManager = default!;

        public SmartFridgeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var smartFridgeSys = EntMan.System<SmartFridgeSystem>();

            _cachedInventory = smartFridgeSys.GetAllInventory(Owner);

            _menu = new SmartFridgeMenu { Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName };

            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;

            _menu.Populate(_cachedInventory, out _cachedFilteredIndex);


            _menu.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            //разобраться, что это?
            //if (state is not SmartFridgeInterfaceState newState)
            //    return;

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, _menu.SearchBar.Text);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {

            var smartFridgeSys = EntMan.System<SmartFridgeSystem>();

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            SendPredictedMessage(new SmartFridgeInteractWithItemEvent(selectedItem.EntityUids[0]));
            SendPredictedMessage(new StorageInteractWithItemEvent(selectedItem.EntityUids[0]));
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
            _menu.Dispose();
        }

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
        }
    }

}
