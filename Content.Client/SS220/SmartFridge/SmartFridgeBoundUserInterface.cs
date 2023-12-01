using Content.Client.SS220.SmartFridge.UI;
using Content.Shared.Storage;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using Content.Shared.Actions;
using Robust.Shared.GameObjects;
using Content.Client.SS220.SmartFridge;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Shared.GameObjects;

namespace Content.Client.SS220.SmartFridge
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

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {

            var smartFridgeSys = EntMan.System<SmartFridgeSystem>();

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            //SendPredictedMessage(new SmartFridgeInteractWithItemEvent(selectedItem.EntityUids[0]));
            SendPredictedMessage(new StorageInteractWithItemEvent(selectedItem.EntityUids[0]));

            _cachedInventory = smartFridgeSys.GetAllInventory(Owner);
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex);
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
