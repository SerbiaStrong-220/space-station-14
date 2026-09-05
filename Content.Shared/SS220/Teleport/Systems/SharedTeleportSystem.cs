// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Ghost;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.SS220.Grab;
using Robust.Shared.Map;

namespace Content.Shared.SS220.Teleport.Systems;

/// <summary>
///     Performs teleportation and raises its common lifecycle events.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedGrabSystem _grab = default!;

    public bool TryTeleport(EntityUid teleporter, EntityUid target, EntityCoordinates destination)
    {
        if (!Exists(teleporter))
            return false;

        if (!Exists(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        var beforeTeleport = new BeforeTeleportEvent(target);
        RaiseLocalEvent(teleporter, ref beforeTeleport);

        StopPullingRelationships(target);
        StopGrabRelationships(target);

        _transform.SetCoordinates(target, Transform(target), destination);

        var targetTeleportedEvent = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref targetTeleportedEvent);

        var teleportedEvent = new TeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref teleportedEvent);

        return true;
    }

    /// <summary>
    ///     Teleports an entity without raising teleport lifecycle events or applying their associated effects.
    /// </summary>
    public bool TryTeleportGhost(EntityUid target, EntityCoordinates destination)
    {
        if (!TryComp<GhostComponent>(target, out _))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        _transform.SetCoordinates(target, Transform(target), destination);
        return true;
    }

    private void StopPullingRelationships(EntityUid target)
    {
        if (TryComp(target, out PullableComponent? targetPullable))
            _pulling.TryStopPull(target, targetPullable);

        if (!TryComp(target, out PullerComponent? targetPuller))
            return;

        if (targetPuller.Pulling is not { } pulledTarget)
            return;

        if (!TryComp(pulledTarget, out PullableComponent? pulledEntity))
            return;

        _pulling.TryStopPull(pulledTarget, pulledEntity);
    }

    private void StopGrabRelationships(EntityUid target)
    {
        if (TryComp(target, out GrabbableComponent? targetGrabbable))
            _grab.BreakGrab((target, targetGrabbable));

        if (!TryComp(target, out GrabberComponent? targetGrabber))
            return;

        if (targetGrabber.Grabbing is not { } grabbedTarget)
            return;

        if (!TryComp(grabbedTarget, out GrabbableComponent? grabbedEntity))
            return;

        _grab.BreakGrab((grabbedTarget, grabbedEntity));
    }
}
