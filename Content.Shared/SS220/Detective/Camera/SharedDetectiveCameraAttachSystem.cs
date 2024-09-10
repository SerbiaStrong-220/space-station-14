// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;

namespace Content.Shared.SS220.Detective.Camera;

public abstract class SharedDetectiveCameraAttachSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    protected bool TryAttachCamera(EntityUid target, Entity<DetectiveCameraAttachComponent> cameraEntity, EntityUid user)
    {
        if (cameraEntity.Comp.Attached)
            return false;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user,
            cameraEntity.Comp.AttachTime,
            new DetectiveCameraAttachDoAfterEvent(target),
            cameraEntity,
            target: cameraEntity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    protected bool TryDetachCamera(EntityUid target, Entity<DetectiveCameraAttachComponent> cameraEntity, EntityUid user)
    {
        if (!cameraEntity.Comp.Attached)
            return false;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user,
            cameraEntity.Comp.DetachTime,
            new DetectiveCameraDetachDoAfterEvent(target),
            cameraEntity,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
}
