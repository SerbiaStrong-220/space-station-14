// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery.Graph;

public static class SharedSurgeryAvaibilityChecks
{
    public static bool IsSurgeryGraphAvailablePerformer(EntityUid target, SurgeryGraphPrototype graph, IEntityManager entityManager)
    {
        foreach (var condition in graph.PerformerAvailabilityCondition)
        {
            if (!IsAvailable(target, condition, entityManager, out _))
                return false;
        }
        return true;
    }

    public static bool IsSurgeryGraphAvailableTarget(EntityUid target, SurgeryGraphPrototype graph, IEntityManager entityManager, out string? reason)
    {
        reason = null;
        foreach (var condition in graph.TargetAvailabilityCondition)
        {
            if (!IsAvailable(target, condition, entityManager, out reason))
                return false;
        }
        return true;
    }

    private static bool IsAvailable(EntityUid target, IAbstractSurgeryGraphAvailabilityCondition condition,
                                        IEntityManager entityManager, out string? reason)
    {
        if (!condition.Condition(target, entityManager, out reason))
            return false;

        return true;
    }
}
