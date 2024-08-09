// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Nyarlathotep;

/// <summary>
/// Searches for entities within a given radius to further pursue them
/// </summary>
public sealed class NyarlathotepTargetSearcherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NyarlathotepSystem _nyarlathotep = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NyarlathotepSearchTargetsComponent, MapInitEvent>(OnSearchMapInit);
    }

    private void OnSearchMapInit(Entity<NyarlathotepSearchTargetsComponent> component, ref MapInitEvent args)
    {
        component.Comp.NextSearchTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Comp.SearchMaxInterval);
    }

    /// <summary>
    /// Updates the target seeker's cooldowns.
    /// Periodically checks for new targets in the radius.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NyarlathotepSearchTargetsComponent>();
        while (query.MoveNext(out var uid, out var targetSearcher))
        {
            if (targetSearcher.NextSearchTime > _gameTiming.CurTime)
                continue;

            TargetSearch(uid, targetSearcher);
            var delay = TimeSpan.FromSeconds(_random.NextFloat(targetSearcher.SearchMinInterval, targetSearcher.SearchMaxInterval));
            targetSearcher.NextSearchTime += delay;
        }
    }

    private void TargetSearch(EntityUid uid, NyarlathotepSearchTargetsComponent component)
    {
        _nyarlathotep.SearchNearNyarlathotep(uid, component.SearchRange);
    }
}
