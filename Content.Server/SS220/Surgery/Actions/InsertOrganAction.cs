// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Surgery.Actions;

/// <summary>
/// Inserts the used organ into the patient. Edge requirements must ensure that the used item is an organ of the category.
/// </summary>
[DataDefinition]
public sealed partial class InsertOrganAction : ISurgeryGraphEdgeAction
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    public void PerformAction(EntityUid targetUid, EntityUid userUid, EntityUid? usedUid, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<BodySystem>();
        if (usedUid is not { } organ || !bodySystem.IsOrganOfCategory(organ, Category))
            return;

        bodySystem.TryInsertOrgan(targetUid, organ);
    }
}
