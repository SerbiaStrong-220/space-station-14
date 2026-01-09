// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Medical;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Systems;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class TryCompactDefibZapAction : ISurgeryGraphEdgeAction
{
    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        if (usedUid is null)
        {
            entityManager.System<SharedSurgerySystem>().Log.Error($"Tried to perform {nameof(TryCompactDefibZapAction)} without any item used to perform");
            return;
        }

        entityManager.System<DefibrillatorSystem>().TryStartZap(usedUid.Value, targetUid, userUid);
    }
}
