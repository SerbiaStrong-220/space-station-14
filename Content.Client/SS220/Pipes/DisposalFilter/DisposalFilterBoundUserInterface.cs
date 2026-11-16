using Content.Shared.SS220.Pipes.DisposalFilter;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Pipes.DisposalFilter;

[UsedImplicitly]
public sealed class DisposalFilterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private DisposalFilterWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<DisposalFilterWindow>();

        _window.OnConfirm += OnConfirm;
    }

    private void OnConfirm(List<DisposalFilterRule> dirByRules, Direction baseDir)
    {
        SendMessage(new DisposalFilterBoundMessage(dirByRules, baseDir));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is DisposalFilterBoundState filter)
        {
            _window.FilterRules = filter.DirByRules;
            _window.BaseDir = filter.BaseDir;
            _window.Populate();
        }
    }
}
