// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Chat;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Shared.CCVar;
using Content.Shared.Chat;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private void AddChannelSpeechBubble(ChatMessage message)
    {
        var presentation = ChatChannelPresentation.ForChannel(message.Channel);
        if (presentation.BubbleType == null)
            return;

        if (message.Channel == ChatChannel.Dead && _ghost?.IsGhost != true)
            return;

        if (message.Channel == ChatChannel.LOOC && !_config.GetCVar(CCVars.LoocAboveHeadShow))
            return;

        if (presentation.BubbleRange != null)
        {
            if (_transform == null)
                return;

            // Message recipients are selected by the server, including private channels such as telepathy.
            if (!EntityManager.TryGetEntity(message.SenderEntity, out var sender))
                return;

            var position = _transform.GetMapCoordinates(sender.Value);
            if (!position.InRange(_eye.CurrentEye.Position, presentation.BubbleRange.Value))
                return;
        }

        AddSpeechBubble(message, presentation.BubbleType.Value);
    }

    /// <summary>
    /// Resolves the focused input channel using the same prefix precedence as message submission.
    /// </summary>
    public ChatSelectChannel GetFocusedChatChannel()
    {
        foreach (var box in _chats)
        {
            if (!box.ChatInput.Input.HasKeyboardFocus())
                continue;

            var channel = SplitInputContents(box.ChatInput.Input.Text).chatChannel;
            return channel == ChatSelectChannel.None ? box.SelectedChannel : channel;
        }

        return ChatSelectChannel.None;
    }

    private void RefreshChatTyping(ChatBox box, ChatSelectChannel prefixChannel)
    {
        if (!box.ChatInput.Input.HasKeyboardFocus())
            return;

        var channel = prefixChannel == ChatSelectChannel.None ? box.SelectedChannel : prefixChannel;
        _typingIndicator?.RefreshChatChannel(channel);
    }

    /// <summary>
    /// Tracks changes only in the focused input. The channel's presentation controls indicator visibility.
    /// </summary>
    public void NotifyChatTextChange(ChatBox box)
    {
        if (!box.ChatInput.Input.HasKeyboardFocus())
            return;

        NotifyChatTextChange();
    }
}
