// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Server.Medical;
using Content.Server.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class TryCompactDefibZapAction : ISurgeryGraphAction
{

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        if (used is null)
        {
            entityManager.System<SurgerySystem>().Log.Error($"Tried to perform {nameof(TryCompactDefibZapAction)} without any item used to perform");
            return;
        }

        entityManager.System<DefibrillatorSystem>().TryStartZap(used.Value, targetUid, userUid);
    }
}
