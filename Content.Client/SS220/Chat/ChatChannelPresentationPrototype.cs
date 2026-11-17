// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Chat.UI;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Chat;

/// <summary>
/// Client presentation settings for a chat channel. Prototype IDs match the names in ChatChannel.
/// Recipient selection, ghost restrictions and user preferences are enforced separately.
/// </summary>
[Prototype]
public sealed partial class ChatChannelPresentationPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Whether focused input may display a typing indicator.
    /// </summary>
    [DataField]
    public bool ShowTyping { get; private set; }

    /// <summary>
    /// The speech bubble style, or null to disable bubbles.
    /// </summary>
    [DataField]
    public SpeechBubble.SpeechType? BubbleType { get; private set; }

    /// <summary>
    /// Maximum distance from the camera when adding a bubble, or null for no extra limit.
    /// </summary>
    [DataField]
    public float? BubbleRange { get; private set; }
}
