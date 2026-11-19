// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

/// <summary>
/// For a body checks that it has an organ of the category, for any other subject (e.g. used item) checks that it is such an organ.
/// </summary>
[DataDefinition]
public sealed partial class HasOrganRequirement : SurgeryGraphRequirement
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField(required: true)]
    public LocId OrganName;

    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
    {
        if (uid is null)
            return false;

        var bodySystem = entityManager.System<BodySystem>();

        if (entityManager.HasComponent<BodyComponent>(uid.Value))
            return bodySystem.HasOrganCategory(uid.Value, Category);

        return bodySystem.IsOrganOfCategory(uid.Value, Category);
    }

    public override string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(Description, ("organ", Loc.GetString(OrganName)));
    }

    public override string RequirementFailureReason(EntityUid? uid, IPrototypeManager prototypeManager, IEntityManager entityManager)
    {
        return Loc.GetString(FailureMessage, ("organ", Loc.GetString(OrganName)));
    }
}
