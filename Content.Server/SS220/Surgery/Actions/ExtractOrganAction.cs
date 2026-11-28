// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

[DataDefinition]
public sealed partial class ExtractOrganAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<BodySystem>();
        if (!bodySystem.TryExtractOrgan(targetUid, Category, out var extractedOrgan))
            return;

        var handsSystem = entityManager.System<SharedHandsSystem>();
        if (handsSystem.TryPickupAnyHand(userUid, extractedOrgan.Value.Owner))
            return;

        var transformSystem = entityManager.System<SharedTransformSystem>();
        transformSystem.DropNextTo(extractedOrgan.Value.Owner, targetUid);
    }
}
