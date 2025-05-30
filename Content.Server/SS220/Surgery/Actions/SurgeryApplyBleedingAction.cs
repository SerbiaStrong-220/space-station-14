// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class ApplyBleedingSurgeryAction : ISurgeryGraphAction
{
    [DataField]
    public float BleedAmount = 2f;

    public void PerformAction(EntityUid uid, EntityUid? userUid, EntityUid? used, IEntityManager entityManager)
    {
        entityManager.System<BloodstreamSystem>().TryModifyBleedAmount(uid, BleedAmount);
    }
}
