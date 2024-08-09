// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Content.Shared.SS220.CultYogg.Components;

namespace Content.Client.SS220.CultYogg.UI
{
    [UsedImplicitly]
    public sealed class MiGoErectBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private MiGoErectMenu? _menu;
        private EntityUid _owner;
        //private List<SiliconLaw>? _laws;

        public MiGoErectBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _owner = owner;
        }

        protected override void Open()
        {
            base.Open();

            _menu = new();

            _menu.OnClose += Close;
            _menu.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;
            _menu?.Close();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not MiGoErectBuiState msg)
                return;

            _menu?.Update(_owner, msg);
        }
    }
}
