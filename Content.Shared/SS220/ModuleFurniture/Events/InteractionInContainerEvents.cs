using Content.Shared.Interaction;

/// <summary>
///     Raised when an entity is activated in the world being in container.
/// Just a copy of <see cref="ActivateInWorldEvent"/> for containers
/// </summary>
public sealed class ActivateInContainerEvent : HandledEntityEventArgs, ITargetedInteractEventArgs
{
    /// <summary>
    ///     Entity that activated the target world entity.
    /// </summary>
    public EntityUid User { get; }

    /// <summary>
    ///     Entity that was activated in the world.
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    ///     Whether or not <see cref="User"/> can perform complex interactions or only basic ones.
    /// </summary>
    public bool Complex;

    /// <summary>
    ///     Set to true when the activation is logged by a specific logger.
    /// </summary>
    public bool WasLogged { get; set; }

    public ActivateInContainerEvent(EntityUid user, EntityUid target, bool complex)
    {
        User = user;
        Target = target;
        Complex = complex;
    }
}
