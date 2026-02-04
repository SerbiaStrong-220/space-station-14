// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class RemovePathologyAction : ISurgeryGraphEdgeAction
{
    [DataField]
    public ProtoId<PathologyPrototype> Pathology;

    public void PerformAction(EntityUid uid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        entityManager.System<SharedPathologySystem>().TryRemovePathology(uid, Pathology);
    }
}
