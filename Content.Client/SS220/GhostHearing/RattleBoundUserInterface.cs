using Content.Shared.SS220.Trigger;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.GhostHearing;

[UsedImplicitly]
public sealed class RattleBoundUserInterface : BoundUserInterface
{
    private GhostHearingWindow? _window;

    public RattleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GhostHearingWindow>();

        _window.OnChannelToggled += (channel, enabled) =>
        {
            SendMessage(new RattleChannelToggledMessage(channel, enabled));
        };

        _window.OnAllChannelToggled += enabled =>
        {
            SendMessage(new RattleToggleAllChannelsMessage(enabled));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not RattleBoundUiState rattleState)
            return;

        _window.SetChannels(rattleState.Channels);
    }

}
