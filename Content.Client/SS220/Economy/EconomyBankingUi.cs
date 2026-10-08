// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Fragments;
using Content.Shared.SS220.Economy;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Economy;

public sealed partial class EconomyBankingUi : UIFragment
{
    private EconomyBankingUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new EconomyBankingUiFragment();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not EconomyBankingCartridgeUiState bankingState)
            return;

        _fragment?.UpdateState(bankingState);
    }
}
