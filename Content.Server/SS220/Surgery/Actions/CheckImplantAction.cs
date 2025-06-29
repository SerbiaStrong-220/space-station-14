// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Graph;

namespace Content.Server.SS220.Surgery.Action;

[DataDefinition]
public sealed partial class CheckImplantAction : ISurgeryGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid userUid, EntityUid? used, IEntityManager entityManager)
    {
        if (used is null)
            return;

        entityManager.System<ImplantCheckInSurgerySystem>().MakeImplantCheckPaper(userUid, used.Value, uid);
    }
}
