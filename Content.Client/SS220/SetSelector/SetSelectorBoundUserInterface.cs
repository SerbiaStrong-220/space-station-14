using Content.Shared.SS220.SetSelector;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.SetSelector;

[UsedImplicitly]
public sealed class SetSelectorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SetSelectorMenu? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SetSelectorMenu>();
        _window.OnApprove += () => SendMessage(new SetSelectorApproveMessage());
        _window.OnSetChange += index => SendMessage(new SetSelectorChangeSetMessage(index));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SetSelectorBoundUserInterfaceState current)
            _window?.UpdateState(current);
    }
}
