using Content.Shared.SS220.Headset;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Headset;

[UsedImplicitly]
public sealed partial class HeadsetBoundUserInterface : BoundUserInterface
{
    private HeadsetWindow? _window;

    public HeadsetBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HeadsetWindow>();

        _window.OnChannelToggled += (channel, enabled) =>
        {
            SendMessage(new HeadsetChannelToggledMessage(channel, enabled));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not HeadsetBoundInterfaceState s)
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
