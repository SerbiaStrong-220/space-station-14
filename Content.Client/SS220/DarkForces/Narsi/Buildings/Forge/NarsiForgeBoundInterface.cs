using System.Linq;
using Content.Shared.SS220.DarkForces.Narsi.Buildings;
using Robust.Shared.Prototypes;
using NarsiCultCraftReceiptCategoryPrototype = Content.Shared.SS220.DarkForces.Narsi.Craft.NarsiCultCraftReceiptCategoryPrototype;

namespace Content.Client.SS220.DarkForces.Narsi.Buildings.Forge;

public sealed class NarsiForgeBoundInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private NarsiForgeWindow? _window;
    private readonly List<NarsiCultCraftReceiptCategoryPrototype> _receipts = default!;
    public NarsiForgeBoundInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _receipts = _prototypeManager.EnumeratePrototypes<NarsiCultCraftReceiptCategoryPrototype>().ToList();
    }

    protected override void Open()
    {
        base.Open();

        _window?.Dispose();

        _window = new NarsiForgeWindow(this, Owner);
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NarsiForgeUIState narsiForgeUIState || _window == null)
            return;

        _window.UpdateState(narsiForgeUIState, _receipts);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }

    public void OnCreateButtonPressed(string itemToSpawn, string requiredMaterial, int cost)
    {
        SendMessage(new NarsiForgeCreateItemEvent(itemToSpawn, requiredMaterial, cost));
    }
}
