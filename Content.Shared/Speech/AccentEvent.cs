namespace Content.Shared.Speech;

public sealed class AccentGetEvent : EntityEventArgs
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

    public AccentGetEvent(EntityUid entity, string message)
    {
        Entity = entity;
        Message = message;
    }
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
