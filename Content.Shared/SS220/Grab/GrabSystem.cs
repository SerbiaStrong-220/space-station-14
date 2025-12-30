namespace Content.Shared.SS220.Grab;

// Baby steps for a bigger system to come
// This is a system separate from PullingSystem due to their different purposes: PullingSystem is meant just to pull things around and GrabSystem is designed for combat
// Current hacks:
// - The control flow comes from PullingSystem 'cuz of input handling
public sealed partial class GrabSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // entry point of any mechanics of grabs
    public void TryDoGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabber, ref grabber.Comp))
            return;
        if (!Resolve(grabbable, ref grabbable.Comp))
            return;

        if (grabbable.Comp.GrabStage == GrabStage.None)
        {
            DoInitialGrab((grabber, grabber.Comp), (grabbable, grabbable.Comp), GrabStage.Passive);
            return;
        }

        UpgradeGrab((grabber, grabber.Comp), (grabbable, grabbable.Comp));
    }

    private void DoInitialGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage grabStage)
    {
        grabber.Comp.Grabbing = grabbable;
        grabbable.Comp.GrabbedBy = grabber;
        grabbable.Comp.GrabStage = grabStage;

        _transform.SetParent(grabbable, grabber);
        _transform.SetLocalPosition(grabbable, grabber.Comp.GrabOffset);
        _transform.SetLocalRotation(grabbable, Angle.Zero);
    }

    private void UpgradeGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable)
    {
        RefreshGrabResistance((grabbable, grabbable.Comp));
        grabbable.Comp.GrabStage++;
    }

    public void RefreshGrabResistance(Entity<GrabbableComponent?> grabbable)
    {

    }
}
