// SS220 changeling Apex tracker
using Content.Shared.Changeling.Mutations;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.UI;

[UsedImplicitly]
public sealed class ChangelingApexTrackerBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private ChangelingApexTrackerWindow? _window;
    private List<ChangelingApexTargetEntry> _targets = [];

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ChangelingApexTrackerWindow>();
        _window.OnTargetSelected += OnTargetSelected;
        _window.SetTargets(_targets);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ChangelingApexTrackerUiState apexState)
            return;

        _targets = apexState.Targets;
        _window?.SetTargets(_targets);
    }

    protected override void Dispose(bool disposing)
    {
        if (_window != null)
            _window.OnTargetSelected -= OnTargetSelected;

        _window?.Close();
        base.Dispose(disposing);
    }

    private void OnTargetSelected(uint selectionToken)
    {
        SendMessage(new ChangelingApexTargetSelectedMessage(selectionToken));
    }
}
