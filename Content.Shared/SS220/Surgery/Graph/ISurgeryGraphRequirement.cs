// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery.Graph;

public interface ISurgeryGraphRequirement
{
    abstract bool SatisfiesRequirements(EntityUid performerUid, EntityUid? targetUid, EntityUid? usedUid, IEntityManager entityManager);

    abstract string RequirementDescription(EntityUid performerUid, EntityUid? targetUid, EntityUid? usedUid, IEntityManager entityManager);

    abstract string RequirementFailureReason(EntityUid performerUid, EntityUid? targetUid, EntityUid? usedUid, IEntityManager entityManager);
}

