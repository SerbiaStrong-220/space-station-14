// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class SurgeryPathologyRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<PathologyPrototype>> CurePathologies = new();

    [DataField]
    public int StackChange = -1;

    /// <summary>
    /// If set, the pathology is lowered to its first level with <see cref="RegressStackCount"/> stacks instead of being cured,
    /// e.g. a repaired organ goes from any failure stage back to damaged and heals by itself.
    /// </summary>
    [DataField]
    public bool RegressLevel;

    [DataField]
    public int RegressStackCount = 1;

    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
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

            if (RegressLevel)
            {
                entityManager.System<SharedPathologySystem>().TryRegressPathology(uid.Value, key, RegressStackCount, toLevel: 0);
                break;
            }

            if (!entityManager.System<SharedPathologySystem>().TryRemovePathology(uid.Value, key))
                entityManager.System<SharedPathologySystem>().TryChangePathologyStack(uid.Value, key, StackChange);

            break;
        }
    }
}
