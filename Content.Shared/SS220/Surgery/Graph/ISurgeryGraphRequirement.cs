// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Graph;

public abstract class SurgeryGraphRequirement
{
    public abstract bool SatisfiesRequirements(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager);

    [MustCallBase(true)]
    public virtual bool MeetRequirement(EntityUid targetUid, EntityUid toolUid, EntityUid userUid, IEntityManager entityManager)
    {
        return SatisfiesRequirements(targetUid, toolUid, userUid, entityManager);
    }

    public abstract string RequirementDescription(IPrototypeManager prototypeManager, IEntityManager entityManager);
}

