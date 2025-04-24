// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Buckle;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Surgery.Components;

namespace Content.Shared.SS220.Surgery.Graph;

public static class SharedSurgeryAvaibilityChecks
{
    // TODO:
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
        var buckle = entityManager.System<SharedBuckleSystem>();

        if (!entityManager.HasComponent<MobStateComponent>(target)
            || !entityManager.HasComponent<SurgableComponent>(target))
            return false;

        if (!buckle.IsBuckled(target))
        {
            reason = "surgery-invalid-target-buckle";
            return false;
        }

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
