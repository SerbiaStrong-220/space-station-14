// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Telepathy;

namespace Content.Shared.Chat.TypingIndicator;

public sealed partial class TypingChangedEvent
{
    /// <summary>
    /// The input is using telepathy. The server derives the channel and recipients from the sender's component.
    /// </summary>
    public bool IsTelepathy { get; init; }
}

public abstract partial class SharedTypingIndicatorSystem
{
    private TypingIndicatorState GetPublicTypingState(EntityUid uid, TypingChangedEvent message)
    {
        var ev = new TelepathyTypingChangedEvent(uid, message.State, message.IsTelepathy);
        RaiseLocalEvent(ref ev);
        return message.IsTelepathy ? TypingIndicatorState.None : message.State;
    }
}
