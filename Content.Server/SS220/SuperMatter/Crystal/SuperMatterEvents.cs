// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.SuperMatterCrystal;

/// <summary>
/// Raised when somebody tried to active supermatter crystal and it wasnt already activated.
/// </summary>
public sealed class SuperMatterActivationEvent(EntityUid performer, EntityUid target) : HandledEntityEventArgs
{
    public EntityUid Performer = performer;
    public EntityUid Target = target;
}
/// <summary>
/// Raised after applying damage to crystal. Wont be raised when crystal delaminate. Only if integrity changed.
/// </summary>
public sealed class SuperMatterIntegrityChanged(float integrity) : EntityEventArgs
{
    public float Integrity { get; } = integrity;
}
/// <summary>
/// Raised when delaminate starts and give start time of destroying, which can be changed later.
/// To receive time updates see <see cref="SuperMatterDelaminateTimeChanged"/>
/// </summary>
public sealed class SuperMatterDelaminateStarted(TimeSpan destroyTime) : EntityEventArgs
{
    /// <summary>
    /// As a reminder it could change, check <see cref="SuperMatterDelaminateTimeChanged"/>
    /// </summary>
    public TimeSpan DestroyTime { get; } = destroyTime;
}
/// <summary>
/// Raised when delamination stops. Not when destroyed.
/// </summary>
public sealed class SuperMatterDelaminateStopped() : EntityEventArgs { }
/// <summary>
/// Raised when delamination stops. Not when destroyed
/// </summary>
public sealed class SuperMatterDelaminateDestroyed() : EntityEventArgs { }
/// <summary>
/// Raised when delamination time changed. Mostly due to crews' works.
/// </summary>
public sealed class SuperMatterDelaminateTimeChanged(TimeSpan newDestroyTime) : EntityEventArgs
{
    public TimeSpan DestroyTime { get; } = newDestroyTime;
}
