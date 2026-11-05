using Content.Shared.SS220.Grab;

namespace Content.Server.SS220.Grab;

public sealed partial class GrabSystem : SharedGrabSystem
{
    public override void Update(float frameTime)
    {
        // don't predict this until you know how to stop networking overriding predicted results
        // it may work with small steps but in the case of grabs the gap between old and new position
        // is highly noticable and looks laggy

        var query = EntityQueryEnumerator<GrabberComponent>();
        while (query.MoveNext(out var uid, out var grabber))
        {
            if (grabber.Grabbing is not { } grabbableUid)
                continue;

            if (!Exists(grabbableUid))
                continue;

            if (!_grabbableQuery.TryComp(grabbableUid, out var grabbableComp))
                continue;

            PlaceGrabbable((uid, grabber), (grabbableUid, grabbableComp));
        }
    }
}
