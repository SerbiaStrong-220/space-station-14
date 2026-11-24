// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.SS220.Body;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

/// <summary>
/// The brain of the subject has a congenital trait to cure.
/// </summary>
[DataDefinition]
public sealed partial class CongenitalTraitRequirement : SurgeryGraphRequirement
{
    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
    {
        if (uid is null || !entityManager.System<BodySystem>().TryGetOrganByCategory(uid.Value, InitialBodySystem.BrainCategory, out var brain))
            return false;

        return entityManager.TryGetComponent<CongenitalTraitsComponent>(brain, out var congenital) && congenital.Traits.Count > 0;
    }
}
