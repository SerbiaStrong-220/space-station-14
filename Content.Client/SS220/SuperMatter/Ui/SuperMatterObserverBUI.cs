// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Client.SS220.SuperMatter.Ui;

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
