// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
        // TODO
        // We have fuckup-likish defib system where all comes in, so copy-paste could be a headache
        // Idea is to make defibish thing to make a good work around. Hope Ill have time
        // entityManager.System<SurgeryResuscitateSystem>().Resuscitate(uid, userUid, Heal);
    }
}
