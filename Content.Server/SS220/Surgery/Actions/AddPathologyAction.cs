// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class AddPathologyAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public ProtoId<PathologyPrototype> Pathology;

    [DataField]
    public int Stacks = 1;

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        entityManager.System<SharedPathologySystem>().TryAddOrStackPathology(targetUid, Pathology, Stacks);
    }
}
