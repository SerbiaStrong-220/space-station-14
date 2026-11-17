// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;

namespace Content.Shared.SS220.Chat;

/// <summary>
/// Reports an attached sender's typing state and selected chat channel to local subscribers.
/// </summary>
[ByRefEvent]
public readonly record struct ChatTypingChangedEvent(
    EntityUid Sender,
    TypingIndicatorState State,
    ChatChannel Channel);
