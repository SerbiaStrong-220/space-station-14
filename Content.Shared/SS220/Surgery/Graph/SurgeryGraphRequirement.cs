// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Surgery.Graph;

public abstract partial class SurgeryGraphRequirement
{
    [DataField(required: true)]
    public SurgeryGraphRequirementSubject Subject = SurgeryGraphRequirementSubject.Target;

    public abstract bool SatisfiesRequirements(EntityUid? uid, IEntityManager entityManager);

    public abstract string RequirementDescription(EntityUid? uid, IEntityManager entityManager);

    public abstract string RequirementFailureReason(EntityUid? uid, IEntityManager entityManager);
}

public enum SurgeryGraphRequirementSubject : int
{
    Target,
    Performer,
    Used,
    Container
}
