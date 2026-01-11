// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class HavePathologyRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public ProtoId<PathologyPrototype> Pathology;

    protected override bool Requirement(EntityUid? uid, IEntityManager entityManager)
    {
        if (uid is null)
            return true;

        return entityManager.System<PathologySystem>().HavePathology(uid.Value, Pathology);
    }
}
