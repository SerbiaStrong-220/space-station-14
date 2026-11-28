// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class RemovePathologyAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public HashSet<ProtoId<PathologyPrototype>> Pathologies = new();

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        var pathologySystem = entityManager.System<SharedPathologySystem>();
        foreach (var pathologyId in Pathologies)
        {
            if (!pathologySystem.HavePathology(targetUid, pathologyId))
                continue;

            pathologySystem.TryRemovePathology(targetUid, pathologyId, checkStacks: false);
        }
    }
}
