// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;
using Content.Shared.SS220.SuperMatter.Ui;

namespace Content.Client.SS220.Cartridges;

public sealed partial class SupermatterObserverUi : UIFragment
{
    private SupermatterObserverUiFragment? _fragment;
    public bool IsInitd => _isInitd;
    private bool _isInitd = false;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }
    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new SupermatterObserverUiFragment();

        _fragment.OnServerButtonPressed += (args, observerComp) =>
            {
                if (args.Button.Pressed)
                    _fragment.Observer = observerComp;
                else
                    _fragment.Observer = null;
                _fragment.CrystalKey = null;
                _fragment.LoadCrystal();
            };
        _fragment.OnCrystalButtonPressed += (args, crystalKey) =>
            {
                if (args.Button.Pressed)
                    _fragment.CrystalKey = crystalKey;
                else
                    _fragment.CrystalKey = null;
                _fragment.LoadCachedData();
            };
    }
    public void DirectUpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case SuperMatterObserverInitState msg:
                _fragment?.LoadState(msg.ObserverEntity);
                _isInitd = true;
                break;
            case SuperMatterObserverUpdateState msg:
                _fragment?.UpdateState(msg);
                break;
        }
    }
    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case SuperMatterObserverInitState msg:
                _fragment?.LoadState(msg.ObserverEntity);
                break;
            case SuperMatterObserverUpdateState msg:
                _fragment?.UpdateState(msg);
                break;
        }
    }
}
