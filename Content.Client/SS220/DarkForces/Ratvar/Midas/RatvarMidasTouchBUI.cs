using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client.SS220.DarkForces.Ratvar.Midas;

public sealed class RatvarMidasTouchBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private RatvarMidasTouchMenu? _menu;
    public RatvarMidasTouchBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new RatvarMidasTouchMenu(this);
        _menu.OnClose += Close;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void Select(string id)
    {
        SendMessage(new RatvarTouchSelectedMessage(id));
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RatvarMidasTouchBUIState castState)
            return;

        _menu?.Populate(castState.Ids);
    }
}
