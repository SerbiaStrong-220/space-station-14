using Content.Shared.SS220.DarkForces.Ratvar.UI;

namespace Content.Client.SS220.DarkForces.Ratvar.Structures;

public sealed class RatvarWorkshopBUI : BoundUserInterface
{
    private RatvarWorkshopWindow _window = new();

    public RatvarWorkshopBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _window.OnCraftPressed += (entityProduce, brass, power, craftTime) =>
            SendMessage(new RatvarWorkshopCraftSelected(entityProduce, brass, power, craftTime));
        _window.OnClose += Close;
    }

    protected override void Open()
    {
        base.Open();

        if (State != null)
        {
            UpdateState(State);
        }

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RatvarWorkshopUIState newState)
            return;

        _window.UpdateState(newState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window.Dispose();
        }
    }
}
