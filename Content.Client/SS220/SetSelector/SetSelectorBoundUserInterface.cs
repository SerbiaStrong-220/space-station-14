using Content.Shared.SS220.SetSelector;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.SetSelector;

[UsedImplicitly]
public sealed class SetSelectorBoundUserInterface : BoundUserInterface
{
    private SetSelectorMenu? _window;

    public SetSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SetSelectorMenu>();
        _window.OnApprove += SendApprove;
        _window.OnSetChange += SendChangeSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SetSelectorBoundUserInterfaceState current)
            return;

        _window?.UpdateState(current);
    }

    public void SendChangeSelected(int setNumber)
    {
        SendMessage(new SetSelectorChangeSetMessage(setNumber));
    }

    public void SendApprove()
    {
        SendMessage(new SetSelectorApproveMessage());
    }
}
