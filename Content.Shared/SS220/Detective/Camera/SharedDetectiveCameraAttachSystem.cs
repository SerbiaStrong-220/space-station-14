// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;

namespace Content.Shared.SS220.Detective.Camera;

public abstract class SharedDetectiveCameraAttachSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    protected bool TryAttachCamera(EntityUid target, DetectiveCameraAttachComponent component, EntityUid user)
    {
        if (component.Attached)
            return false;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user,
            component.AttachTime,
            new DetectiveCameraAttachDoAfterEvent(target),
            component.Owner,
            target: component.Owner)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    protected bool TryDetachCamera(EntityUid target, DetectiveCameraAttachComponent component, EntityUid user)
    {
        if (!component.Attached)
            return false;

        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user,
            component.DetachTime,
            new DetectiveCameraDetachDoAfterEvent(target),
            component.Owner,
            target: component.Owner)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            Hidden = true
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
}
