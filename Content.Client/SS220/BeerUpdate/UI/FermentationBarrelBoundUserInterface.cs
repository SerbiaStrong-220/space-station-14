using Content.Shared.SS220.BeerUpdate.FermentationBarrel;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.BeerUpdate.UI;

public sealed class FermentationBarrelBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FermentationBarrelMenu? _menu;

    public FermentationBarrelBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<FermentationBarrelMenu>();
        _menu.OnStartStop += OnStartStop;
        _menu.OnModeChange += OnModeChange;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not FermentationBarrelInterfaceState cState)
            return;

        _menu?.UpdateState(cState.IsActive, cState.ElapsedTime, cState.Reagents, cState.IsDrawMode, cState.TotalSolution, cState.MaxSolution);
    }

    private void OnStartStop()
    {
        SendMessage(new FermentationBarrelToggleEvent());
    }

    private void OnModeChange()
    {
        SendMessage(new FermentationBarrelModeChangeEvent());
    }
}
