// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat.TypingIndicator;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// Separate from public appearance data so the server can restrict telepathic typing to channel recipients.
/// Added to every entity with a typing indicator, so its presence does not reveal telepathy.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TelepathyTypingIndicatorComponent : Component
{
    [ViewVariables]
    public TypingIndicatorState State;

    public override bool SessionSpecific => true;
}

[Serializable, NetSerializable]
public sealed class TelepathyTypingIndicatorState(TypingIndicatorState state) : ComponentState
{
    public readonly TypingIndicatorState State = state;
}

/// <summary>
/// Routes validated sender identity and input mode from the ordinary typing event to the private indicator.
/// </summary>
[ByRefEvent]
public readonly record struct TelepathyTypingChangedEvent(EntityUid Sender, TypingIndicatorState State, bool IsTelepathy);
