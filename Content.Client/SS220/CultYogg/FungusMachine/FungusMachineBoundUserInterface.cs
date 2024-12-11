using Content.Shared.SS220.CultYogg.FungusMachine;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.CultYogg.FungusMachine.UI
{
    public sealed class FungusMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private FungusMachineMenu? _menu;

        [ViewVariables]
        private List<FungusMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        public FungusMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var fungusMachineSys = EntMan.System<SharedFungusMachineSystem>();

            _cachedInventory = fungusMachineSys.GetInventory(Owner);

            _menu = this.CreateWindow<FungusMachineMenu>();
            _menu.OpenCenteredLeft();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;

            _menu.Populate(_cachedInventory, out _cachedFilteredIndex);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not FungusMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, _menu.SearchBar.Text);
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            SendMessage(new FungusSelectedId(selectedItem.Id));
        }

        private void OnSearchChanged(string? filter)
        {
            _menu?.Populate(_cachedInventory, out _cachedFilteredIndex, filter);
        }
    }
}
