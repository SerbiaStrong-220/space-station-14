// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

[DataDefinition]
public sealed partial class HasOrganRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    protected override bool Requirement(EntityUid? uid, EntityUid user, IEntityManager entityManager)
    {
        if (uid is null)
            return false;

        var bodySystem = entityManager.System<BodySystem>();

        if (Subject == SurgeryGraphRequirementSubject.Performer)
        {
            var handsSystem = entityManager.System<SharedHandsSystem>();
            foreach (var held in handsSystem.EnumerateHeld(uid.Value))
            {
                if (bodySystem.IsOrganOfCategory(held, Category))
                    return true;
            }

            return false;
        }

        if (entityManager.HasComponent<BodyComponent>(uid.Value))
            return bodySystem.HasOrganCategory(uid.Value, Category);

        return bodySystem.IsOrganOfCategory(uid.Value, Category);
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(Description, ("organ", Category.Id));
    }

    public override string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(FailureMessage, ("organ", Category.Id));
    }
}
