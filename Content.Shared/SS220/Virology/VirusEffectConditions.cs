// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle.Components;
using Content.Shared.Standing;

namespace Content.Shared.SS220.Virology;

public static class VirusEffectConditions
{
    /// <summary>True if the carrier is lying or buckled.</summary>
    public static bool IsRecumbent(EntityUid uid, IEntityManager entityManager)
    {
        if (entityManager.System<StandingStateSystem>().IsDown(uid))
            return true;

        return entityManager.TryGetComponent<BuckleComponent>(uid, out var buckle) && buckle.Buckled;
    }
}
