// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Chat;

namespace Content.Shared.Chat.TypingIndicator;

public abstract partial class SharedTypingIndicatorSystem
{
    private TypingIndicatorState GetPublicTypingState(EntityUid uid, TypingChangedEvent message)
    {
        var ev = new ChatTypingChangedEvent(uid, message.State, message.Channel);
        RaiseLocalEvent(ref ev);
        return message.Channel == ChatChannel.Telepathy ? TypingIndicatorState.None : message.State;
    }
}
