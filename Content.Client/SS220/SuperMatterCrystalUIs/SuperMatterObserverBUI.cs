namespace Content.Client.SS220.SuperMatterCrystalUIs;

public sealed class SuperMatterObserverBUI : BoundUserInterface
{
    [ViewVariables]
    private SuperMatterObserverMenu? _menu;

    public SuperMatterObserverBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }
    protected override void Open()
    {
        base.Open();

        _menu = new SuperMatterObserverMenu();
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        _menu?.UpdateState();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
        _menu?.Dispose();
    }
}
