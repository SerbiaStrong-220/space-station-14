using Content.Shared.SS220.GhostHearing;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.GhostHearing;

[UsedImplicitly]
public sealed partial class GhostHearingBoundUserInterface : BoundUserInterface
{
    private GhostHearingWindow? _window;

    public GhostHearingBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GhostHearingWindow>();

        _window.OnChannelToggled += (channel, enabled) =>
        {
            SendMessage(new GhostHearingChannelToggledMessage(channel, enabled));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not GhostHearingBoundUIState s)
            return;

        _window.SetChannels(s.Channels);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }

}
