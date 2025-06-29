// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.SuperMatter.Ui;

public sealed class SuperMatterObserverBUI : BoundUserInterface
{
    [ViewVariables]
    private SuperMatterObserverMenu? _menu;

    public SuperMatterObserverBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }
    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SuperMatterObserverMenu>();

        _menu.OnServerButtonPressed += (args, observerComp) =>
        {
            if (args.Button.Pressed)
                _menu.Observer = observerComp;
            else
                _menu.Observer = null;
            _menu.CrystalKey = null;
            _menu.LoadCrystal();
        };
        _menu.OnCrystalButtonPressed += (args, crystalKey) =>
        {
            if (args.Button.Pressed)
                _menu.CrystalKey = crystalKey;
            else
                _menu.CrystalKey = null;
            _menu.LoadCachedData();
        };
    }
    public void DirectUpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case SuperMatterObserverInitState msg:
                _menu?.LoadState(msg.ObserverEntity);
                break;
            case SuperMatterObserverUpdateState msg:
                _menu?.UpdateState(msg);
                break;
        }
    }
}
