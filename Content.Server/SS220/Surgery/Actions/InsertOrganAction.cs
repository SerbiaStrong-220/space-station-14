// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class InsertOrganAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<BodySystem>();
        if (usedUid is { } heldOrgan && bodySystem.IsOrganOfCategory(heldOrgan, Category))
        {
            bodySystem.TryInsertOrgan(targetUid, heldOrgan);
            return;
        }

        var handsSystem = entityManager.System<SharedHandsSystem>();
        foreach (var heldEntity in handsSystem.EnumerateHeld(userUid))
        {
            if (!bodySystem.IsOrganOfCategory(heldEntity, Category))
                continue;

            bodySystem.TryInsertOrgan(targetUid, heldEntity);
            return;
        }
    }
}
