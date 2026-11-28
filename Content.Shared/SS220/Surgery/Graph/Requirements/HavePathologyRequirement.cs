// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class HavePathologyRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public ProtoId<PathologyPrototype> Pathology;

    /// <summary>
    /// Pathology level must be at least this, e.g. only organ failure is repairable by surgery.
    /// </summary>
    [DataField]
    public int MinLevel;

    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        if (!entityManager.TryGetComponent<PathologyHolderComponent>(uid, out var holder)
            || !holder.ActivePathologies.TryGetValue(Pathology, out var data))
            return false;

        return data.Level >= MinLevel;
    }
}
