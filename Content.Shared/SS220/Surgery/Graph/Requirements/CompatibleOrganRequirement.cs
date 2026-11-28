// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;

namespace Content.Shared.SS220.Surgery.Graph.Requirements;

/// <summary>
/// Checks on the patient that the used organ fits the patient's species, see <see cref="InitialBodySystem.IsOrganCompatible"/>.
/// </summary>
[DataDefinition]
public sealed partial class CompatibleOrganRequirement : SurgeryGraphRequirement
{
    protected override bool Requirement(EntityUid? uid, EntityUid user, EntityUid? used, IEntityManager entityManager)
    {
        if (uid is null || used is not { } organ)
            return false;

        return entityManager.System<InitialBodySystem>().IsOrganCompatible(uid.Value, organ);
    }
}
