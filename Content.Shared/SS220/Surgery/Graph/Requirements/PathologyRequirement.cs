// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class PathologyRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<PathologyPrototype>> CurePathologies = new();

    [DataField]
    public int StackChange = -1;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<PathologyHolderComponent>(uid, out var pathologyHolder))
            return false;

        foreach (var (key, _) in pathologyHolder.ActivePathologies)
        {
            if (CurePathologies.Contains(key))
                return true;
        }

        return false;
    }

    protected override void AfterRequirementMet(EntityUid? uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<PathologyHolderComponent>(uid, out var pathologyHolder))
            return;

        foreach (var (key, _) in pathologyHolder.ActivePathologies)
        {
            if (!CurePathologies.Contains(key))
                continue;

            if (!entityManager.System<SharedPathologySystem>().TryRemovePathology(uid.Value, key))
                entityManager.System<SharedPathologySystem>().TryChangePathologyStack(uid.Value, key, StackChange);

            break;
        }
    }
}
