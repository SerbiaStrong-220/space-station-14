// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.MindSlave.Systems;
using Content.Shared.SS220.MindSlave;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class SurgeryFixMindSlaveDisfunctionAction : ISurgeryGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, EntityUid? used, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<MindSlaveDisfunctionFixerComponent>(used, out var fixer))
        {
            entityManager.System<MindSlaveDisfunctionSystem>().Log.Fatal($"Surgery step with mind slave fix successfully done without used having {nameof(MindSlaveDisfunctionFixerComponent)}");
            return;
        }

        entityManager.System<MindSlaveDisfunctionSystem>().WeakDisfunction(uid, fixer.DelayMinutes, fixer.RemoveAmount);
    }
}
