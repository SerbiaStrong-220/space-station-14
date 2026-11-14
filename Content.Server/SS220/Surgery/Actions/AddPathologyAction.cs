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
    public int StackCount = 1;

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        var pathologySystem = entityManager.System<SharedPathologySystem>();
        if (!pathologySystem.HavePathology(targetUid, Pathology))
        {
            pathologySystem.TryAddPathology(targetUid, Pathology);
            if (StackCount > 1)
                pathologySystem.TryChangePathologyStack(targetUid, Pathology, StackCount - 1);
            return;
        }

        pathologySystem.TryChangePathologyStack(targetUid, Pathology, StackCount);
    }
}
