using System.Numerics;
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

        var recenter = !EntMan.System<SharedUserInterfaceSystem>().TryGetPosition(Owner, UiKey, out _);

        _window = this.CreateWindow<SetSelectorMenu>();
        _window.OnApprove += Approve;
        _window.OnSetChange += index => SendPredictedMessage(new SetSelectorChangeSetMessage(index));
        Update();

        if (!recenter)
            return;

        _window.Measure(Vector2Helpers.Infinity);
        _window.RecenterWindow(new Vector2(0.5f, 0.5f));
    }

    public override void Update()
    {
        if (_window == null || !EntMan.TryGetComponent<SetSelectorComponent>(Owner, out var selector))
            return;

        _window.Update(selector.PossibleSets, selector.SelectedSets, selector.MaxSelectedSets, selector.ToolName, selector.ToolDesc);
    }

    private void Approve()
    {
        SendMessage(new SetSelectorApproveMessage());
        Close();
    }
}
