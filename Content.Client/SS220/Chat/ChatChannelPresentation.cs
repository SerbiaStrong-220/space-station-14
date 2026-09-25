// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Chat.UI;
using Content.Shared.Chat;

namespace Content.Client.SS220.Chat;

/// <summary>
/// Client presentation defaults for a chat channel. These do not grant permission to send or receive messages.
/// </summary>
/// <param name="ShowTyping">Whether focused input may display a typing indicator.</param>
/// <param name="BubbleType">The speech bubble style, or null to disable bubbles.</param>
/// <param name="BubbleRange">Maximum distance from the camera when adding a bubble, or null for no extra limit.</param>
public readonly record struct ChatChannelPresentation(
    bool ShowTyping,
    SpeechBubble.SpeechType? BubbleType = null,
    float? BubbleRange = null)
{
    /// <summary>
    /// Returns the presentation defaults for a single channel. Unlisted channels have no indicators or bubbles.
    /// Recipient selection, ghost restrictions and user preferences are enforced separately.
    /// </summary>
    public static ChatChannelPresentation ForChannel(ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Local => new(true, SpeechBubble.SpeechType.Say),
            ChatChannel.Whisper => new(true, SpeechBubble.SpeechType.Whisper),
            ChatChannel.Radio => new(true),
            ChatChannel.Dead => new(true, SpeechBubble.SpeechType.Say),
            ChatChannel.Emotes => new(true, SpeechBubble.SpeechType.Emote),
            ChatChannel.LOOC => new(false, SpeechBubble.SpeechType.Looc),
            ChatChannel.AdminChat => new(true),
            ChatChannel.Unspecified => new(true),
            ChatChannel.Telepathy => new(true, SpeechBubble.SpeechType.Say, SharedChatSystem.VoiceRange),
            _ => default,
        };
    }
}
