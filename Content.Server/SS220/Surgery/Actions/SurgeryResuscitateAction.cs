// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Surgery.ActionSystems;
using Content.Shared.Damage;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class SurgeryResuscitateAction : ISurgeryGraphAction
{
    [DataField(required: true)]
    public DamageSpecifier Heal = default!;

    public void PerformAction(EntityUid uid, EntityUid? userUid, EntityUid? used, IEntityManager entityManager)
    {
        entityManager.System<SurgeryResuscitateSystem>().Resuscitate(uid, userUid, Heal);
    }
}
