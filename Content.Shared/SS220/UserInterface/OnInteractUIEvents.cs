// Highly inspired by ActivatableUIEvents all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.UserInterface;

public sealed class InteractUIOpenAttemptEvent(EntityUid who) : CancellableEntityEventArgs
{
    public EntityUid User { get; } = who;
}

public sealed class UserOpenInteractUIAttemptEvent(EntityUid who, EntityUid target) : CancellableEntityEventArgs //have to one-up the already stroke-inducing name
{
    public EntityUid User { get; } = who;
    public EntityUid Target { get; } = target;
}

public sealed class BeforeInteractUIOpenEvent(EntityUid who, EntityUid target) : EntityEventArgs
{
    public EntityUid User { get; } = who;
    public EntityUid Target { get; } = target;
}

public sealed class AfterInteractUIOpenEvent(EntityUid who, EntityUid target, EntityUid actor) : EntityEventArgs
{
    public EntityUid User { get; } = who;
    public EntityUid Target { get; } = target;
    public readonly EntityUid Actor = actor;
}


public sealed class InteractUIPlayerChangedEvent : EntityEventArgs
{
}
