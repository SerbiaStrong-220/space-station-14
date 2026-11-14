// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Chat;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Robust.Shared.Prototypes;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ChatSelectChannel _inputChannel;
    private bool _typingUpdateQueued;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_typingUpdateQueued)
            return;

        if (!_time.InPrediction)
            return;

        if (!_time.IsFirstTimePredicted)
            return;

        _typingUpdateQueued = false;
        ClientUpdateTyping();
    }

    /// <summary>
    /// Stores the effective input channel supplied by the UI and updates the indicator when it changes.
    /// </summary>
    public void RefreshChatChannel(ChatSelectChannel channel)
    {
        if (_inputChannel == channel)
            return;

        _inputChannel = channel;
        ClientUpdateTyping();
    }

    /// <summary>
    /// Updates the input channel before the focus change emits a typing event.
    /// </summary>
    public void ClientChangedChatFocus(bool isFocused, ChatSelectChannel channel)
    {
        _inputChannel = channel;
        ClientChangedChatFocus(isFocused);
    }

    private void SendTypingChangedEvent(TypingIndicatorState state)
    {
        // Replicated CVar changes may run while applying server state, outside prediction.
        if (!_time.InPrediction)
        {
            _typingUpdateQueued = true;
            return;
        }

        if (!_time.IsFirstTimePredicted)
        {
            _typingUpdateQueued = true;
            return;
        }

        _typingUpdateQueued = false;

        var channel = (ChatChannel)_inputChannel;
        var showTyping = _prototypeManager.TryIndex<ChatChannelPresentationPrototype>(
            channel.ToString(), out var presentation) && presentation.ShowTyping;

        if (!showTyping || !_cfg.GetCVar(CCVars.ChatShowTypingIndicator))
            state = TypingIndicatorState.None;

        RaisePredictiveEvent(new TypingChangedEvent(state)
        {
            Channel = channel,
        });
    }
}
