using Robust.Shared.Timing;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This handles...
/// </summary>
public sealed class SharedContractorTargetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ContractorTargetComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EnteredPortalTime == TimeSpan.Zero)
                return;

            if (comp.EnteredPortalTime + comp.TimeInJail < _timing.CurTime)
            {
                _transform.SetCoordinates(uid, comp.PortalPosition);
                comp.EnteredPortalTime = TimeSpan.Zero;
                RemComp<ContractorTargetComponent>(uid);
            }
        }
    }
}
