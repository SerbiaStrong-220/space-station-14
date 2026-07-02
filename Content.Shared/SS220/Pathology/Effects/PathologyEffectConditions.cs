// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Standing;

namespace Content.Shared.SS220.Pathology.Effects;

/// <summary>Shared gate checks reused by per-tick pathology effects.</summary>
public static class PathologyEffectConditions
{
    /// <summary>True if the host is off its feet — lying down (Standing.IsDown) or buckled (sitting/strapped).</summary>
    public static bool IsRecumbent(EntityUid uid, IEntityManager entityManager)
    {
        if (entityManager.System<StandingStateSystem>().IsDown(uid))
            return true;

        return entityManager.TryGetComponent<BuckleComponent>(uid, out var buckle) && buckle.Buckled;
    }
}
