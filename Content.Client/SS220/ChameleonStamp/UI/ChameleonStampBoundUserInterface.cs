// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChameleonStamp;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.ChameleonStamp.UI;

[UsedImplicitly]
public sealed partial class ChameleonStampBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ChameleonStampMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<ChameleonStampMenu>();
        _menu.OnIdSelected += OnIdSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChameleonStampBoundUserInterfaceState st)
            return;

        _menu?.UpdateState(st.Variants, st.SelectedId);
    }

    private void OnIdSelected(EntProtoId selectedId)
    {
        SendMessage(new ChameleonStampSelectedMessage(selectedId));
    }
}