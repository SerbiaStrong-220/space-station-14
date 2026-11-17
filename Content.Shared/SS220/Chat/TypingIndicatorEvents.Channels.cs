// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.Chat.TypingIndicator;

public sealed partial class TypingChangedEvent
{
    /// <summary>
    /// The input channel selected by the sender. The server enforces recipient restrictions separately.
    /// </summary>
    public ChatChannel Channel { get; init; }
}
