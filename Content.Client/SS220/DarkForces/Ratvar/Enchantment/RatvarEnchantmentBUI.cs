using Content.Shared.SS220.DarkForces.Ratvar.UI;
using Robust.Client.Graphics;
using Robust.Client.Input;

namespace Content.Client.SS220.DarkForces.Ratvar.Enchantment;

public sealed class RatvarEnchantmentBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private RatvarEnchantmentMenu? _menu;

    public RatvarEnchantmentBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new RatvarEnchantmentMenu(this);
        _menu.OnClose += Close;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void Select(string id, string visuals)
    {
        SendMessage(new RatvarEnchantmentSelectedMessage(id, visuals));
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

        if (state is not RatvarEnchantmentBUIState castState)
            return;

        _menu?.PopulateRadial(castState.Models);
    }
}
