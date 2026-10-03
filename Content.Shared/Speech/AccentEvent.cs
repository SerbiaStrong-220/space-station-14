using Content.Shared.Inventory;

namespace Content.Shared.Speech;

/// <summary>
///     Raised on an entity to apply speech accents to its message.
///     Handlers should modify <see cref="Message"/> in place.
///     Relayed through inventory (e.g. voice masks) and status effects.
/// </summary>
[ByRefEvent]
public record struct AccentGetEvent(EntityUid Entity, string Message) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    // SS220 Made cancellable for accent check begin
    public bool Cancelled { get; private set; }

    public void Cancel() => Cancelled = true;
    // SS220 Made cancellable for accent check end
}

// SS220 Mindslave-stop-word begin
/// <summary>
/// Raised before common accent event. Used for complex replacement.
/// </summary>
public sealed class BeforeAccentGetEvent : EntityEventArgs
{
    /// <summary>
    ///     The entity to apply the accent to.
    /// </summary>
    public EntityUid Entity { get; }

    /// <summary>
    ///     The message to apply the accent transformation to.
    ///     Modify this to apply the accent.
    /// </summary>
    public string Message { get; set; }

    public BeforeAccentGetEvent(EntityUid entity, string message)
    {
        Entity = entity;
        Message = message;
    }
}
// SS220 Mindslave-stop-word end
